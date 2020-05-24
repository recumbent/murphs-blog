// Script to create a new blog post in drafts folder
open System
open System.IO

// First build the filname 
let filename = 
    fsi.CommandLineArgs
    |> Array.tail
    |> Array.map (fun s -> s.ToLower())
    |> String.concat "-"

// Make sure we've got a filename...
if String.IsNullOrWhiteSpace filename
then printfn "A new post requires a title"
else
    if not (Directory.Exists "drafts") then
        Directory.CreateDirectory("drafts") |> ignore

    let title = 
        fsi.CommandLineArgs
        |> Array.tail
        |> String.concat " "
    
    let content = [|
        "---"
        "layout: post"
        (sprintf "title: %s" title)
        "author: @recumbent"
        "tags:" 
        "---"
    |]
    
    let filePath = sprintf "drafts/%s.md" filename
    File.WriteAllLines(filePath, content)

    printfn "New post file created as: %s" filePath

