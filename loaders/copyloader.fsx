#r "../_lib/Fornax.Core.dll"

open System.IO

let loader (projectRoot: string) (siteContent: SiteContents) =
    let intputPath = Path.Combine(projectRoot, "images")
    let outputPath = Path.Combine(projectRoot, "_public", "images")
    if Directory.Exists outputPath then Directory.Delete(outputPath, true)
    Directory.CreateDirectory outputPath |> ignore

    for dirPath in Directory.GetDirectories(intputPath, "*", SearchOption.AllDirectories) do
        Directory.CreateDirectory(dirPath.Replace(intputPath, outputPath)) |> ignore

    for filePath in Directory.GetFiles(intputPath, "*.*", SearchOption.AllDirectories) do
        File.Copy(filePath, filePath.Replace(intputPath, outputPath), true)
    siteContent