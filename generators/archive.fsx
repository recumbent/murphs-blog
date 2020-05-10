#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open Html

let generate' (ctx : SiteContents) (page: string) =

    Layout.layout ctx "Archive" [
        section [] [
            h1 [] [!! "Murph's Blog - full archive"]
            p [] [!! "This will be a complete list of all the posts, when I fix the home page to be better too..."]
        ]
    ]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> Layout.render ctx