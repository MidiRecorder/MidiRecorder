properties {
    $split = "$version" -split "-",2
    $mainversion = $split[0]
}

function Check() {
    param (
        [ScriptBlock]$block
    )
    
    $block.Invoke();
    if ($lastexitcode -ne 0)
    {
        throw "Command-line program failed: $($block.ToString())"
    }
}

FormatTaskName ""

task default -Depends TestParams

Task Init {
    Assert ($version -ne $null) '$version should not be null'
    Assert ($actor -ne $null) '$actor should not be null'
    Set-Location "$PSScriptRoot/src"
}

Task Clean -Depends Init {
    if (Test-Path ./CommandLine/nupkg/) { rm -r -fo ./CommandLine/nupkg/ }
}

Task Build -Depends Clean {
    "🏭 Restore dependencies"
    Check { dotnet restore }
    "🧱 Build"
    Check {
        dotnet build `
            /p:Version="$version" `
            /p:AssemblyVersion="$mainversion" `
            /p:FileVersion="$mainversion" `
            /p:InformationalVersion="$version" `
            --no-restore
    }
}

Task UnitTests -Depends Build {
    "🐛 Unit Tests"
    Check { dotnet test --no-build --logger:"console;verbosity=normal" }
}

Task Pack -Depends Build {
    "📦 NuGet Pack"
    Check { dotnet pack /p:PackageVersion="$version" --no-build }
}

Task Push -Depends Pack,UnitTests {
    "📢 NuGet Push"
    Check {
        dotnet nuget push `
            ./CommandLine/nupkg/*.nupkg `
            --api-key $env:NUGET_AUTH_TOKEN `
            --source https://api.nuget.org/v3/index.json }
}

Task TagCommit -Depends Push {
    "🏷 Tag commit and push"
    Check { git config --global user.email "$actor@users.noreply.github.com" }
    Check { git config --global user.name "$actor" }
    Check { git tag v$version }
    Check { git push origin --tags }
}

Task Publish -Depends TagCommit
