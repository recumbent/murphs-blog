#r "../_lib/Fornax.Core.dll"
#load "layout.fsx"

open Html

let generate' (ctx : SiteContents) (page: string) =

    Layout.layout ctx "About" [
        section [] [
            h1 [] [!! "About Murph's Blog"]
            p [] [!! "All thoughts my own, more later when I can work out the right way to create this page..."]
        ]
    ]

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> Layout.render ctx