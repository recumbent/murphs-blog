---
author: @recumbent
tags: github actions, fornax, azure
---

As a rule, the very first thing I would do for a given project is set up the end to end pipeline to build, test, and deploy a "hello world!" implementation to production (if you have dev/test/staging environments then this should be the full pipeline with quality gates as needed). This is often described as a "pathfinder" release and once created ensures that you can iterate on the project quickly deploying as you go.

I chose not to do that for this blog because the most important thing is the content - and playing with build technologies would be another reason to not actually put anything live. Now its time to fix that.

## Problem statement

As such things go this is a relatively straightforward problem - we can run the build steps with a simple commands and deployment is a matter of copying one folder.

Keeping things simple, I want to redeploy on a commit to master (more usually a push to master).

## Where are we now

First a recap on what I'm currently using:

1. BLog is built using [Fornax](https://github.com/ionide/fornax) installed as a _local_ tool
1. Which depends on the dotnet sdk
1. Deployed to an [azure static website](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website)
1. Versioned in a git repository hosted on [github](https://github.com/recumbent/) 

## What needs to be done

To do a basic build and deploy of the website requires the following steps (ish)

1. Checkout the repository
1. Do `dotnet tool restore`
1. Do `dotnet fornax build`
1. Copy the contents of the `_public` folder to azure blob storage

The two `dotnet` steps require the .NET Core SDK, copying the files to azure requires AzCopy, and somewhere along the way secrets are going to be involved.

We only care about master - so if master changes then we want to run the steps as above.

## Choices

There are any number of ways I could run this (CircleCI, Azure Dev Ops, etc) but this is a public repo hosted on Github - so the sensible choice is [Github Actions](https://github.com/features/actions) - and as this is something I haven't used before its an opportunity to learn.

Strictly I would treat this as at least two separate jobs in a workflow, with build/test/package separate from publish/deploy - but this doesn't need that level of complexity (yet) so we'll keep it as simple as possible.

## Enough witering already...

Github offers a guided experience for setting up actions - if you go to the repo and click on the action tab here:

![Action tab in github repository](//images/github-actions/actions-001.png)

This prompted me with this:

![Get started with GitHub Actions](//images/github-actions/actions-002.png)

Which looked _close enough_ to give me a place to start - so I clicked on "Set up this workflow" and that gave me an editor with this "wall of yaml":

```yml
name: .NET Core

on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]

jobs:
  build:

    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET Core
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 3.1.101
    - name: Install dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --configuration Release --no-restore
    - name: Test
      run: dotnet test --no-restore --verbosity normal
```

Now I've done a lot of work with (CircleCI)[https://circleci.com] which means I'm experienced with walls of yaml then therefore the above makes sense to me and its certainly close to what I need.

Checkout - yup, setup .NET core - yup, we have a file so we'll start by saving it, I changed the name to `build.yml` here:

![Name github action](//images/github-actions/actions-003.png)

And then commited the file by pushing the big green `Start Commit` button toward the top right of the ui:

![Start commit dialog](//images/github-actions/actions-004.png)

And... that not only saves the workflow, it also runs it...

![Start commit dialog](//images/github-actions/actions-005.png)

And thirty-some seconds later it fails - which is probably reasonable...

The problem is with `Run dotnet restore` which quite rightly complains: `MSBUILD : error MSB1003: Specify a project or solution file. The current working directory does not contain a project or solution file.`

So lets change the workflow to more closely match our needs, by changing steps to be the following:

```yml
    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET Core
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 3.1.101
    - name: Install dependencies
      run: dotnet tool restore
    - name: Build
      run: dotnet fornax build
```

In the above we've changed the restore and the build and removed the test step as we have no tests (its a static site, there is no code...)

Committing those changes on github immediately triggers another build! Which promptly fails because the build container is case sensitive where windows is not! Fixing that results in a successful build and a log showing the site being generated.





