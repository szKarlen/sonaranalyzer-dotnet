$ErrorActionPreference = "Stop"

function testExitCode(){
    If($LASTEXITCODE -ne 0) {
        write-host -f green "lastexitcode: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

#download MSBuild
    $url = "https://github.com/SonarSource-VisualStudio/sonar-msbuild-runner/releases/download/2.0/MSBuild.SonarQube.Runner-2.0.zip"
    $output = ".\MSBuild.SonarQube.Runner.zip"    
    Invoke-WebRequest -Uri $url -OutFile $output
    unzip -o .\MSBuild.SonarQube.Runner.zip
    testExitCode

if ($env:IS_PULLREQUEST -eq "true") { 
    write-host -f green "in a pull request"

    .\MSBuild.SonarQube.Runner begin /k:sonaranalyzer-csharp-vbnet /n:"SonarAnalyzer for C#" /v:latest `
        /d:sonar.host.url=$env:SONAR_HOST_URL `
        /d:sonar.login=$env:SONAR_TOKEN `
        /d:sonar.github.pullRequest=$env:PULL_REQUEST `
        /d:sonar.github.repository=$env:GITHUB_REPO `
        /d:sonar.github.oauth=$env:GITHUB_TOKEN `
        /d:sonar.analysis.mode=issues `
        /d:sonar.scanAllFiles=true
    testExitCode

    & $env:NUGET_PATH restore .\SonarAnalyzer.sln
    testExitCode
    & $env:MSBUILD_PATH .\SonarAnalyzer.sln /t:rebuild /p:Configuration=Release /p:DeployExtension=false
    testExitCode

    .\MSBuild.SonarQube.Runner end /d:sonar.login=$env:SONAR_TOKEN
    testExitCode

} else {
    if (($env:GITHUB_BRANCH -eq "master") -or ($env:GITHUB_BRANCH -eq "refs/heads/master")) {
        write-host -f green "Building master branch"

        #setup Nuget.config
        del $env:APPDATA\NuGet\NuGet.Config
        & $env:NUGET_PATH sources Add -Name repox -Source https://repox.sonarsource.com/api/nuget/sonarsource-nuget-qa/
        testExitCode
        $apikey = $env:ARTIFACTORY_DEPLOY_USERNAME+":"+$env:ARTIFACTORY_DEPLOY_PASSWORD
        & $env:NUGET_PATH setapikey $apikey -Source repox
        testExitCode
        
        #generate build version from the build number
        $unpaddedBuildversion="$env:BUILD_NUMBER"
        $buildversion="$env:BUILD_NUMBER"
        for($i=1; $i -le 5-$env:BUILD_NUMBER.length; $i++){$buildversion = "0"+$buildversion}

        $branchName = "$env:GITHUB_BRANCH"
        $sha1 = "$env:GIT_SHA1"

        #Append build number to the versions
        (Get-Content .\build\Version.props) -replace '<NugetVersion>\$\(MainVersion\)</NugetVersion>', "<NugetVersion>`$(MainVersion)-build$buildversion</NugetVersion>" | Set-Content .\build\Version.props
        (Get-Content .\build\Version.props) -replace '<AssemblyFileVersion>\$\(MainVersion\)\.0</AssemblyFileVersion>', "<AssemblyFileVersion>`$(MainVersion).$unpaddedBuildversion</AssemblyFileVersion>" | Set-Content .\build\Version.props
        (Get-Content .\build\Version.props) -replace '<AssemblyInformationalVersion>Version:\$\(AssemblyFileVersion\) Branch:not-set Sha1:not-set</AssemblyInformationalVersion>', "<AssemblyInformationalVersion>Version:`$(AssemblyFileVersion) Branch:$branchName Sha1:$sha1</AssemblyInformationalVersion>" | Set-Content .\build\Version.props
        & $env:MSBUILD_PATH  .\build\ChangeVersion.proj
        testExitCode

        #build
        & $env:NUGET_PATH restore .\SonarAnalyzer.sln
        testExitCode
        & $env:MSBUILD_PATH .\SonarAnalyzer.sln /p:configuration=Release /p:DeployExtension=false /p:ZipPackageCompressionLevel=normal /v:m /p:defineConstants=SignAssembly /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=$env:CERT_PATH
        testExitCode

        #Generate the XML descriptor files for the C# plugin
        pushd .\src\SonarAnalyzer.RuleDescriptorGenerator\bin\Release
        .\SonarAnalyzer.RuleDescriptorGenerator.exe cs
        .\SonarAnalyzer.RuleDescriptorGenerator.exe vbnet
        popd

        #generate packages
        $files = Get-ChildItem .\src -recurse *.nuspec
        foreach ($file in $files) {
            $output = $file.directoryname+"\bin\Release"
            & $env:NUGET_PATH pack $file.fullname -NoPackageAnalysis -OutputDirectory $output
            testExitCode
        }

        #get version number
        [xml]$versionProps = Get-Content .\build\Version.props
        $version = $versionProps.Project.PropertyGroup.MainVersion+"-build$buildversion"

        #write version to property file
        $s="VERSION=$version"
        Write-Host "$s"
        $s | out-file -encoding utf8 -append ".\version.properties"
        #convert sha1 property file to unix for jenkins compatiblity
        Get-ChildItem .\version.properties | ForEach-Object {
        $contents = [IO.File]::ReadAllText($_) -replace "`r`n?", "`n"
        $utf8 = New-Object System.Text.UTF8Encoding $false
        [IO.File]::WriteAllText($_, $contents, $utf8)
        }

        #upload packages
        $files = Get-ChildItem src -recurse *.nupkg
        foreach ($file in $files) {    
            #upload to nuget repo 
            & $env:NUGET_PATH push $file.fullname -Source repox
            testExitCode
            #compute artifact name from filename
            $artifact=$file.name.replace($file.extension,"").replace(".$version","")
            $filePath=$file.fullname
            (Get-Content .\poms\$artifact\pom.xml) -replace "file-$artifact", "$filePath" | Set-Content .\poms\$artifact\pom.xml          
        }
        #upload to maven repo        
        cd poms
        write-host -f green  "set version $version in pom.xml"
        $command = "mvn versions:set -DgenerateBackupPoms=false -DnewVersion='$version'"
        iex $command
        write-host -f green  "set version $version in env VAR PROJECT_VERSION for artifactory buildinfo metadata"
        $env:PROJECT_VERSION=$version
        write-host -f green  "set the buildnumber to this job build number"
        $env:BUILD_ID=$env:BUILD_NUMBER
        write-host -f green  "Deploy to repox with $version"    
        $command = 'mvn deploy -Pdeploy-sonarsource -B -e -V'
        iex $command
    } else {
        write-host -f green "not on master"
    }

}



