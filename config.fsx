#r "_lib/Fornax.Core.dll"

open Config
open System.IO

let postPredicate (projectRoot: string, page: string) =
    let fileName = Path.Combine(projectRoot, page)
    let ext = Path.GetExtension page
    if ext = ".md" then
        let ctn = File.ReadAllText fileName
        ctn.Contains("layout: post")
    else
        false

let staticPredicate (projectRoot: string, page: string) =
    let ext = Path.GetExtension page
    not (
        page.Contains ".DS_Store" ||
        page.Contains "_public" ||
        page.Contains ".config" ||
        page.Contains "_lib" ||
        page.Contains ".git" ||
        page.Contains ".ionide" ||
        page.Contains ".vs" ||
        ext = ".fsx"
    )
 
let config = {
    Generators = [
        { Script = "post.fsx"; Trigger = OnFilePredicate postPredicate; OutputFile = ChangeExtension "html" }
        { Script = "monthindex.fsx"; Trigger = Once; OutputFile = MultipleFiles (sprintf "posts/%s/index.html") }
        { Script = "yearindex.fsx"; Trigger = Once; OutputFile = MultipleFiles (sprintf "posts/%s/index.html") }
        { Script = "staticfile.fsx"; Trigger = OnFilePredicate staticPredicate; OutputFile = SameFileName }
        { Script = "index.fsx"; Trigger = Once; OutputFile = NewFileName "index.html" }
        { Script = "about.fsx"; Trigger = Once; OutputFile = NewFileName "about.html" }
        { Script = "archive.fsx"; Trigger = Once; OutputFile = NewFileName "posts/index.html" }
        { Script = "taglist.fsx"; Trigger = Once; OutputFile = NewFileName "tags/index.html"}
    ]
}