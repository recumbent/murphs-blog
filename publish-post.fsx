open System
open System.IO

let (|Int|_|) str =
   match System.Int32.TryParse(str:string) with
   | (true,int) -> Some(int)
   | _ -> None

let printItem index file =
    printfn " %02i - %s" (index + 1) file

let doPublish files index =
    let max = (Array.length files)
    match index with
    | i when i > 0 && i <= max -> 
        printfn "publishing..."
        let publishDate = DateTime.Today.ToString "yyyy-MM-dd"
        let sourceFile = files.[i - 1]
        let fileInf = FileInfo(sourceFile)  
        let target = sprintf "posts/%s-%s" publishDate fileInf.Name
        printfn "Target: %s" target

        let sourceLines = File.ReadAllLines sourceFile
        
        let targetLines = "---" :: ((sprintf "published: %s" publishDate) :: Array.toList (Array.skip 1 sourceLines))
        File.WriteAllLines (target, targetLines)
        fileInf.Delete()

    | _ -> printfn "Please pick a number that actually exists"    

let draftFiles = Directory.GetFiles("drafts")

if Array.isEmpty draftFiles
then printfn "No drafts to publish"
else
    printfn "Select draft to publish:"
    printfn ""
    Array.iteri printItem draftFiles
    printfn ""
    printf "Number: "

    let input = Console.ReadLine()

    match input with
    | Int i -> doPublish draftFiles i
    | _ -> printfn"Not a number!"
