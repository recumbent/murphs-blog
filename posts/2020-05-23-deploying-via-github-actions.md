---
layout: post
title: Deploying this blog via github actions
author: @recumbent
tags: github actions, fornax, azure, how to
published: 2020-05-23
updated: 2020-05-24
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
1. Deployed to an [azure storage blob static website](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website)
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

Time for some useable "stuff"

### Building the site

Github offers a guided experience for setting up actions - if you go to the repo and click on the action tab here:

![Action tab in github repository](/images/github-actions/actions-001.png)

This prompted me with this:

![Get started with GitHub Actions](/images/github-actions/actions-002.png)

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

![Name github action](/images/github-actions/actions-003.png)

And then commited the file by pushing the big green `Start Commit` button toward the top right of the ui:

![Start commit dialog](/images/github-actions/actions-004.png)

And... that not only saves the workflow, it also runs it...

![Start commit dialog](/images/github-actions/actions-005.png)

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

Committing those changes on github immediately triggers another build! Which promptly fails because the build container is case sensitive where windows is not! I got sent a handy email too (links removed):

> ## Run failed for master (da952b4)  
> 
> Repository: recumbent/murphs-blog  
> Workflow: .NET Core  
> Duration: 36.0 seconds  
> Finished: 2020-05-17 15:32:22 UTC  
> 
> View results  
> 
> Jobs:   
> * build failed (1 annotation)  
> 
> You are receiving this because this workflow ran on your branch.  
> Manage your GitHub Actions notifications here.  

Fixing that results in a successful build and a log showing the site being generated.

---

At this point [Build 2020](https://mybuild.microsoft.com/) happened and intevitably they introduced what is probably a better way to host my static web site [Azure Static Web Apps](https://docs.microsoft.com/en-us/azure/static-web-apps/overview) - so at some point I'll probably want to migrate to that (need to understand the https certificate story).

In the meantime I still need to create a pipeline for what I've got so I shall carry on.

---

### Deploying the site

Having created the site I need to run AzCopy to push the content into blob storage.

That in turn means that I need AzCopy in my action - I'm not the first person to want this!

The yaml file that defines the action is now part of the repo: `.github\workflows\build.yml` so I could edit it on my machine, but if I use the editor in github I get more help so I'll do that - if I navigate to the file in github [build.yml](https://github.com/recumbent/murphs-blog/blob/master/.github/workflows/build.yml) and click on the pencil to edit:

![Github edit icon](/images/github-actions/actions-006.png)

I not only get an editor, I also get the ability to search the marketplace for actions:

![Search marketplace tab](/images/github-actions/actions-007.png)

Typing blob into the search gives me lots of options - the first of which was [Azure Blob Storage Upload](https://github.com/marketplace/actions/azure-blob-storage-upload) the second example shows uploading a hugo static site... close enough - lets run with that!

We add a step as follows to copy from `_public` to `$web`:

```yml
    - uses: bacongobbler/azure-blob-storage-upload@v1.1.1
      with:
        source_dir: '_public'
        container_name: '$web'
        connection_string: ${{ secrets.ConnectionString }}
        sync: true
```

Which is all good apart from the connection string... clearly I don't want to share the connection string so one assumes that secrets is there to help me with that.

Saving my changes to the script runs it and it promptly fails: `storage account connection string is not set. Quitting.`

### Setting a secret

Ok, how do I go set a secret?

In my repo, I choose the Settings tab, from there I can choose Secrets from the list on the left.

This tells me:

> Secrets are environment variables that are encrypted and only exposed to selected actions Anyone with collaborator access to this repository can use these secrets in a workflow.

Perfect! I shall click on the "New Secret" button...

I name the secret "ConnectionString", I acquire a connection string (using [Azure Storage Explorer](https://docs.microsoft.com/en-us/azure/vs-azure-tools-storage-manage-with-storage-explorer)) and paste that in then save.

Now I have a secret I can go back and run the action again.

And magic... the log shows us the files uploaded

```
1/30: "LICENSE"[######################################################]  100.0000%
2/30: "about.html"[###################################################]  100.0000%
3/30: "README.md"[####################################################]  100.0000%
4/30: "index.html"[###################################################]  100.0000%
...
```

It also shows that we spend an age (43 precious seconds!) pulling down the docker container to allow us to run the copy (which is done using the azure cli, so there will be python involved). This is not terrible, but given that we already have a very capable general purpose framework installed one might consider writing a script to do the copy instead - although that in turn depends on what azure static web apps offers.

So in summary:

1. The build/deployment process for this blog is straightforward
1. Github actions offers a low friction CI/CD option for projects in github
1. We can take advantage of the work of others to perfom trickier steps (in this case someone else has done the work to copy files to blob storage)
1. We can keep secrets safe
1. There is always a better way 🤔




