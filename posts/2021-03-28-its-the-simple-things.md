---
published: 2021-03-28
layout: post
title: Its the simple things
author: @recumbent
tags: Pulumi,F#,Azure
---

Every so often you write some code to solve a relatively simple problem and that code makes you happy - this is one of those examples. In this case it was the lack of effort involved in getting to the result I needed - I _almost_ wrote the code and had it just work (not quite, there was some polish to get the final result, but close).

## The problem

We need to deploy a desktop client application, the path of least resistance for us, at this point in time, is to use ClickOnce as the tooling is now available as a dotnet tool.

To make this work we need to be able to copy a number of files from our CI pipeline to an azure Blob container (again, this is a "path of least resistance" solution).

I _say_ copy - but since there are something over 240 files and no more than a dozen of those change between releases what I really want is a form of synchronisation (and that gets a bit funky when your CI process starts with a shiny new VM each time).

## The solution

We use [Pulumi](https://pulumi.com) to define our cloud infrastructure and that includes the ability to manage individual files in cloud storage (a blob in this case). This lets me write F# code, leaning on the .NET framework, to solve my problem.

Since F# requires we write code top to bottom, I'll walk through the steps and then show all of the code at the end


### 1. Allow for multiple deployed instances

We need to be able to deploy multiple versions of the application (talking to different back ends) so we name our stacks `<application>-<cloud>-<environment>` e.g. `application-azure-dev` the first step is to parse the stack name:

```fsharp
    // Parse the stack name for useful information
    let components = Pulumi.Deployment.Instance.StackName.Split('-')
    let application = components.[0]
    let cloud = components.[1]
    let targetEnv = components.[2]
```

### 2. Create a resource group

As we're targetting azure we create a resource group for the resources (storage account, container, files) that we're going to create

```fsharp
    // Create an Azure Resource Group
    let resourceGroupName = $"rg-{application}-{targetEnv}"
    let resourceGroup =
        ResourceGroup(resourceGroupName)
```

### 3. Create a storage account

The first resource is a storage account, this will also define the public URL so we need to name it explicitly:

```fsharp
    // Create an Azure Storage Account
    let storageAccountName = $"sa-{application}-{targetEnv}"
    let accountName = $"{targetEnv}{application}murphsco"
    let storageAccount =
        StorageAccount(storageAccountName,
            StorageAccountArgs(
                ResourceGroupName = io resourceGroup.Name,
                Sku = input (SkuArgs(Name = inputUnion2Of2 SkuName.Standard_LRS)),
                Kind = inputUnion2Of2 Kind.StorageV2,
                AccountName = input accountName)
            )
```

In the above we define both a storage account name and an account name. The former defines the resource for Pulumi, the latter forms part of the public URL (so has to be lower case, 24 characters or less, no punction, and globally unique...) so we specify it _explicitly_ in order that the URL is known and deterministic.

We also need the resource group name (taken from the resource group we created, to allow for randomised names), and some details about what sort of blob storage we want.

### 4. Create a container for the deployment files

We'll call the container "deployment" (we might change this to take advantage of static web page hosting in blob storage accounts, but again this is _working_ code so...)

```fsharp
    let container =
        BlobContainer("deployment",
            BlobContainerArgs(
                ResourceGroupName = io resourceGroup.Name,
                AccountName = io storageAccount.Name,
                ContainerName = input "deployment",
                PublicAccess = input PublicAccess.Container
            )
        )
```

In the above we're explicit about the container name and we specifiy annoymous read access to the contents of the container.

### 5. Find the files to deploy

This we do with dotnet:

```fsharp
    let solutionFolder =
        DirectoryInfo(__SOURCE_DIRECTORY__).Parent

    let publishFolder = Path.Combine(solutionFolder.FullName, "AppToDeploy.WPF", "bin", "publish")
    let publishDirectory = DirectoryInfo(publishFolder)

    let files = publishDirectory.EnumerateFiles("*", EnumerationOptions(RecurseSubdirectories = true)) |> List.ofSeq
```

1. Find the solution folder (which is the parent of the directory the Pulumi project is in)
1. Find the folder we targetted with `dotnet pubish` and then post-processed to be "click once"
1. Get a DirectoryInfo object for the publish folder
1. Enumerate the files in the publish folder and below - Microsoft have done the hard work for me [DirectoryInfo.EnumerateFiles Method](https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratefiles?view=net-5.0)
1. I convert the enumeration into a list - notionally a sequence (enumeration) can be infinite, but we know that there should be a finite number of files, and for my needs a list is more useful

### 6. Split the application file from the rest

We want to output the URL for the `.application` file from the stack, as this is the URL one would open to install (and potentially run) the application, we can do this by using `List.partition` to create _two_ lists - one containing the `FileInfo` for the applicationFile and once containing the `FileInfo`s for _all the other files_

```fsharp
    // We need to output the deployment location so pull the "application" file out from the others
    let (app, others) = List.partition (fun (fi : FileInfo) -> fi.Name = "AppToDeploy.application") files
```

`app` should be a list containing a single item, `others` will be a somewhat larger list.

### 7. Define a function to create a blob

I need to go amend the code in my pull request... this is a function to create a blob from a `FileInfo`

```fsharp
    let createBlob (file : FileInfo) =
        let name = Path.GetRelativePath(publishDirectory.FullName, file.FullName)
        Blob(name,
            BlobArgs(
                ResourceGroupName = io resourceGroup.Name,
                AccountName       = io storageAccount.Name,
                ContainerName     = io container.Name,
                BlobName          = input name,
                Source            = input (FileAsset file.FullName :> AssetOrArchive)
            )
        )
```

The clever bit here in the above is to use more .NET magic in [Path.GetRelativePath Method](https://docs.microsoft.com/en-us/dotnet/api/system.io.path.getrelativepath?view=net-5.0) - this gives us the "name" we want for the blob in the form of something that is a root relative path. Then to create the blob we specify the resource group, then the storage account, then the container, the explicit name for the blob and lastly where Pulumi can find the file 

### 8. Sync the files

Call the create blob function...

```fsharp
    // Use exactly one here to ensure we have what we have what we expect
    let deployFileBlob = List.exactlyOne app |> createBlob

    for file in others do
       createBlob file |> ignore
```

We need to know the URL for the deployed file so we capture the result of creating the blob.

We don't care about the rest so run a for loop over the others and ignore the result

If the files don't exist, the Pulumi runtime will create them in blob storage, if they do exist they will be updated is they are different, and if they are no longer needed they will be removed... pretty cool, and about as efficient as we can reasonably hope for.

### 9. Output the deployment URL

The last value in a function is the return value, the Pulumi F# run function expects a dictionary of stack outputs

```fsharp
    // Export the install URL
    dict [("installUrl", deployFileBlob.Url :> obj)]
```

Assuming one has performed the right incantations along the way, if one runs `pulumi up` and copies the URL into a browser there's a fair chance it will attempt to install our application. 

## Which makes me happy because...

Its probably taken me almost as long to type the description of what I did as it did to write the code in the first place - there was some exploration as I found `EnumerateFiles`, I had to look at `GetRelativePath` to see what it gave me (exactly the right thing) and there was a small refactor so that I could use `List.partition` - but I end up with a very small amount of _very_ readable code (steps 5, 6, 7, and 8) to do a non-trivial synchronisation operation in a manner that is re-usable and repeatable.

- F# let me write readable, terse, code
- .NET provided a ridiculously comprehensive set of tools for fundamental operations (in this case working with Directories, Paths, and Files)
- [.NET interactive](https://github.com/dotnet/interactive) in VS Code let me play with the directory and file operations (could have done the same in FSI)
- Pulumi provides magic to do the heavy lifting for cloud infrastructure work



## All the code

The full code is below - its not perfect, I'm going to refactor `createBlob to take the root directory a it first parameter (and there are some _reasonable_ assumptions about the environment in which the code will run), but I'm happy enough to show it to the world 

```fsharp
module Program

open System.IO

open Pulumi.FSharp
open Pulumi.AzureNative.Resources
open Pulumi.AzureNative.Storage
open Pulumi.AzureNative.Storage.Inputs
open Pulumi

let infra () =
    // Parse the stack name for useful information
    let components = Pulumi.Deployment.Instance.StackName.Split('-')
    let application = components.[0]
    let cloud = components.[1]
    let targetEnv = components.[2]

    // Create an Azure Resource Group
    let resourceGroupName = $"rg-{application}-{targetEnv}"
    let resourceGroup =
        ResourceGroup(resourceGroupName)

    // Create an Azure Storage Account
    let storageAccountName = $"sa-{application}-{targetEnv}"
    let accountName = $"{targetEnv}{application}biosigs"
    let storageAccount =
        StorageAccount(storageAccountName,
            StorageAccountArgs(
                ResourceGroupName = io resourceGroup.Name,
                Sku = input (SkuArgs(Name = inputUnion2Of2 SkuName.Standard_LRS)),
                Kind = inputUnion2Of2 Kind.StorageV2,
                AccountName = input accountName)
            )

    let container =
        BlobContainer("deployment",
            BlobContainerArgs(
                ResourceGroupName = io resourceGroup.Name,
                AccountName = io storageAccount.Name,
                ContainerName = input "deployment",
                PublicAccess = input PublicAccess.Container
            )
        )

    let solutionFolder =
        DirectoryInfo(__SOURCE_DIRECTORY__).Parent

    let publishFolder = Path.Combine(solutionFolder.FullName, "AppToDeploy.WPF", "bin", "publish")
    let publishDirectory = DirectoryInfo(publishFolder)

    let files = publishDirectory.EnumerateFiles("*", EnumerationOptions(RecurseSubdirectories = true)) |> List.ofSeq

    // We need to output the deployment location so pull the "application" file out from the others
    let (app, others) = List.partition (fun (fi : FileInfo) -> fi.Name = "AppToDeploy.application") files

    let createBlob (file : FileInfo) =
        let name = Path.GetRelativePath(publishDirectory.FullName, file.FullName)
        Blob(name,
            BlobArgs(
                ResourceGroupName = io resourceGroup.Name,
                AccountName       = io storageAccount.Name,
                ContainerName     = io container.Name,
                BlobName          = input name,
                Source            = input (FileAsset file.FullName :> AssetOrArchive)
            )
        )

    // Use exactly one here to ensure we have what we have what we expect
    let deployFileBlob = List.exactlyOne app |> createBlob

    for file in others do
       createBlob file |> ignore

    // Export the install URL
    dict [("installUrl", deployFileBlob.Url :> obj)]

[<EntryPoint>]
let main _ =
  Deployment.run infra
```
