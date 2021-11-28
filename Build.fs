open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Farmer
open Farmer.Builders

open Helpers

initializeContext()

let root = __SOURCE_DIRECTORY__
let sharedPath = Path.getFullName "src/Shared"
let serverPath = Path.getFullName "src/Server"
let clientPath = Path.getFullName "src/Client"
let deployPath = Path.getFullName "deploy"
let sharedTestsPath = Path.getFullName "tests/Shared"
let serverTestsPath = Path.getFullName "tests/Server"
let clientTestsPath = Path.getFullName "tests/Client"

Target.create "Clean" (fun _ ->
    Shell.cleanDir deployPath
    Shell.cleanDir (clientPath @@ "output")
)

Target.create "InstallClient" (fun _ -> run yarn "install" root)

Target.create "Bundle" (fun _ ->
    [ "server", dotnet $"publish -c Release -o \"{deployPath}\"" serverPath
      "client", yarn "bundle" root]
    |> runParallel
)

Target.create "Azure" (fun _ ->

    let vaultName = "VatsimLiveFeedback-vault"
    let secretRef name = $"@Microsoft.KeyVault(VaultName={vaultName};SecretName={name})"
    let secrets = [
        "VLF_OAuth__ClientId", "oauth-client-id"
        "VLF_OAuth__ClientSecret", "oauth-client-secret"
    ]
    let secretSettings = secrets |> List.map (fun (key,name) -> key, secretRef name)
    let web = webApp {
        name "VatsimLiveFeedback"
        zip_deploy "deploy"
        link_to_keyvault (ResourceName vaultName)
        settings secretSettings
        system_identity
        runtime_stack Runtime.DotNet50
    }

    let emptySecret secretName = secret {
        name secretName
        value (ArmExpression.literal "")
    }

    let vaultUser =
        Environment.environVarOrNone "VLF_KEYVAULT_USER_OBJECTID"
        |> Option.bind (fun s -> match System.Guid.TryParse(s) with true, guid -> ObjectId guid |> Some | _ -> None)

    let vault = keyVault {
        name vaultName
        add_secrets (secrets |> List.map (fun (_, name) -> emptySecret name))
        add_access_policies [
            AccessPolicy.create (web.SystemIdentity, [KeyVault.Secret.List; KeyVault.Secret.Get])
            if vaultUser.IsSome then
                AccessPolicy.create (vaultUser.Value, KeyVault.Secret.All)
        ]
    }

    let deployment = arm {
        location Location.WestEurope
        add_resources [
            web
            vault
        ]
    }

    deployment
    |> Deploy.execute "VatsimLiveFeedback" Deploy.NoParameters
    |> ignore
)

Target.create "Run" (fun _ ->
    run dotnet "build" sharedPath
    [ "server", dotnet "watch run" serverPath
      "client", yarn "start" root ]
    |> runParallel
)

Target.create "RunTests" (fun _ ->
    run dotnet "build" sharedTestsPath
    [ "server", dotnet "watch run" serverTestsPath
      "client", dotnet "fable watch -o output -s --run webpack-dev-server --config ../../webpack.tests.config.js" clientTestsPath ]
    |> runParallel
)

Target.create "Format" (fun _ ->
    run dotnet "fantomas . -r" "src"
)

open Fake.Core.TargetOperators

let dependencies () = [
    "Clean"
        ==> "InstallClient"
        ==> "Bundle"
        ==> "Azure"

    "Clean"
        ==> "InstallClient"
        ==> "Run"

    "InstallClient"
        ==> "RunTests"
]

[<EntryPoint>]
let main args = runOrDefault args dependencies