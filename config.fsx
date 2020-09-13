#r "_lib/Fornax.Core.dll"

open Config
open System
open System.IO

let postPredicate (projectRoot: string, page: string) =
    let fileName = Path.Combine(projectRoot, page)
    let ext = Path.GetExtension page
#if !WATCH
    if not (page.Contains "drafts") && ext = ".md" then
#else 
    if ext = ".md" then
#endif
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
        page.Contains "drafts" ||
        ext = ".fsx" ||
        ext = ".wsp"
    )

let makePostPath (date : string) title =
    let dateParts = date.Split '-'
    let newPath = sprintf "%s/%s/%s/%s" dateParts.[0] dateParts.[1] dateParts.[2] title
    Path.ChangeExtension (newPath, "html" )
    
let postRename (page : string) =
    let elements = page.ToLower().Split '/'
    let root = elements.[0]
    match root with
    | "posts" -> makePostPath (elements.[1].Substring(0, 10)) (elements.[1].Substring(11))
    | _ -> makePostPath (DateTime.Today.ToString("yyyy-MM-dd")) elements.[1]    
    

let config = {
    Generators = [
        { Script = "post.fsx"; Trigger = OnFilePredicate postPredicate; OutputFile = Custom postRename }
        { Script = "monthindex.fsx"; Trigger = Once; OutputFile = MultipleFiles (sprintf "%s/index.html") }
        { Script = "yearindex.fsx"; Trigger = Once; OutputFile = MultipleFiles (sprintf "%s/index.html") }
        { Script = "staticfile.fsx"; Trigger = OnFilePredicate staticPredicate; OutputFile = SameFileName }
        { Script = "index.fsx"; Trigger = Once; OutputFile = NewFileName "index.html" }
        { Script = "about.fsx"; Trigger = Once; OutputFile = NewFileName "about.html" }
        { Script = "archive.fsx"; Trigger = Once; OutputFile = NewFileName "archive.html" }
        { Script = "taglist.fsx"; Trigger = Once; OutputFile = NewFileName "tags/index.html"}
    ]
}