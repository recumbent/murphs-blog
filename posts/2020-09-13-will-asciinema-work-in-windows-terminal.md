---
published: 2020-09-13
layout: post
title: Will Asciinema work in windows terminal
author: @recumbent
tags: Python, Windows Terminal, How To
---

I'm attempting to put together a presentation on [Pulumi](https://www.pulumi.com) for the second [NE-RPC](https://ne-rpc.co.uk/) conference - so am I writing my script or the code to go with? No, I'm wondering if I can use [asciinema] to record working at the command line to embed in the presentation... in fairness to me this is a sensible thing to want to do given that I'm trying to do a pre-recorded session.

## So what's the problem

The basic problem is that "windows" isn't supported because the existing windows console sucks... except that we don't _just_ have the existing windows console any more, now we have Windows Terminal, one that explicitly plays nicer with the "standards" for terminals and so things get a bit more interesting.

At this point there's good news and bad... the good is that asciinema is python - so in theory runnable, the less good is that there is no python on this box.

I've got two options at this point, one is to try running in WSL and the other... well in theory it should "just work" in a terminal window.

## Install Python

Chocolatey is my friend (I could probably do this via a Visual Studio workload too):

```shell
choco install python -y
```

Which is almost perfect (I now have _python_ but I also need _pip_)

This helped: [](https://www.foxinfotech.in/2019/04/how-to-install-pip-on-windows-10.htm)

Long story short... didn't work - asciinema has a python dependency that's not supported on Windows

## If at first...

In this case the answer is to go slightly sideways. My fallback position is to run asciinema under (WSL 2)[https://docs.microsoft.com/en-us/windows/wsl/about] but you can (now) launch a windows app from a linux command line and have it work - so what if we could run asciinema in _linux_ to record powershell running in _windows_

This turns out to straightforward, the hardest part (!) was working out where the powershell executable lives.

I already had Ubuntu installed for WSL, so I made sure python was up to date, and that pip3 was installed and up to date.

Then I did `sudo pip3 install asciinema` - per the installation intructions. That worked pretty much as expected and so I was good to go.

On my machine Powershell Core 7 is installed into `C:\Program Files\PowerShell\7` and the executable is `pwsh.exe`

So I opened up windows terminal, opened a tab to run ubunutu and typed the following:

```shell
asciinema rec
/mnt/c/'Program Files'/PowerShell/7/pwsh.exe
get-location
get-childitem
exit
exit
```

That does the following:

1. Starts recording
2. Opens a windows powershell session in the _same terminal window_ from the linux shell
3. Gets the current location in powershell
4. Lists the files in that location
5. exits from powershell
6. exits from the recording

The result can be seen here: [https://asciinema.org/a/356995]

Hmm, one can do better:

```
asciinema rec --command="/mnt/c/'Program Files'/PowerShell/7/pwsh.exe"
```

Now it launches straight into _windows_ powershell. Neat.

