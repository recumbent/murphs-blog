#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open Html

let generate' (ctx : SiteContents) (page: string) =

    Layout.layout ctx "Tags" [
        section [] [
            h1 [] [!! "Murph's Blog - Tags"]
            p [] [!! "List of tags used, links to per tag index pages, coming at some point"]
        ]
    ]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> Layout.render ctx