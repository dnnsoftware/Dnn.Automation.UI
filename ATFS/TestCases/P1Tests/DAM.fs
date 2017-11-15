// Automation UI Testing Framework - ATFS - http://www.dnnsoftware.com
// Copyright (c) 2015 - 2017, DNN Corporation
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

module DAM

open System.IO
open canopy
open DnnCanopyContext
open DnnAddToRole
open DnnDAM

let private findAndOpenFolder folderName = 
    let folderListSelector = "//div[contains(@class,'jspPane')]/div/ul/li/ul"
    let mutable listItem = 1
    let mutable listItemSelector = folderListSelector + "/li[" + listItem.ToString() + "]"
    let mutable found = false
    while exists listItemSelector && not found do
        let readFolderName = (element listItemSelector).GetAttribute("title")
        if readFolderName = folderName then
            let foundFolderSelector = listItemSelector + "/div/img"
            click foundFolderSelector
            sleep 1
            waitPageLoad()
            found <- true
        listItem <- listItem + 1
        listItemSelector <- folderListSelector + "/li[" + listItem.ToString() + "]"
    found

let private createFolderTest foldertype = 
    let folderName = "TestFolder" + getRandomId()
    createFolderDam folderName foldertype
    if findAndOpenFolder folderName then
        printfn "  PASS: Folder %A was created successfully." folderName
    else
        failwithf "  FAIL: Folder %A could not be created.\n" folderName

let private uploadFileTest filename =
    let filePath = Path.Combine(additionalFilesLocation, filename)
    if uploadFileDam filePath then
        let readFileName = (element fileSpanSelectorDam).GetAttribute("title")
        if readFileName <> filename then
            failwithf "  FAIL: Uploaded file name %A does not match with found file name %A" filename readFileName

let hostTests _ =
    context "DAM: Host Tests"

    "DAM | Host | Create a Standard folder and Upload a PNG file" @@@ fun _ ->
        loginAsHost()
        closePersonaBar()
        goto "/Host/File-Management"
        createFolderTest FolderType.STANDARD
        uploadFileTest "dnn_logo_big.png"

    "DAM | Host | Create a Secure folder and Upload a DOCX file" @@@ fun _ ->
        goto "/Host/File-Management"
        createFolderTest FolderType.SECURE
        uploadFileTest "LoremIpsum.docx"

    "DAM | Host | Create a Database folder and Upload a ZIP file" @@@ fun _ ->
        goto "/Host/File-Management"
        createFolderTest FolderType.DATABASE
        uploadFileTest "TestFiles.zip"   

let adminTests _ =
    context "DAM: Admin Tests"

    "DAM | Admin | Create a Standard folder and Upload a PNG file" @@@ fun _ ->
        loginAsAdmin() |> ignore
        closePersonaBar()
        goto "/Admin/File-Management"
        createFolderTest FolderType.STANDARD
        uploadFileTest "dnn_logo_big.png"

    "DAM | Admin | Create a Secure folder and Upload a DOCX file" @@@ fun _ ->
        goto "/Admin/File-Management"
        createFolderTest FolderType.SECURE
        uploadFileTest "LoremIpsum.docx"

    "DAM | Admin | Create a Database folder and Upload a ZIP file" @@@ fun _ ->
        goto "/Admin/File-Management"
        createFolderTest FolderType.DATABASE
        uploadFileTest "TestFiles.zip"            

let all _ = 
    hostTests()
    adminTests()
