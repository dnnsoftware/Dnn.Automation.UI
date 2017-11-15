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

module RecycleBin

open canopy
open DnnCanopyContext
open DnnManager
open DnnAddToRole
open DnnUserLogin
open DnnRecycleBin

let mutable private pageName = ""

let private adminTests _ =
    context "Recycle Bin : Admin Tests"

    "Recycle Bin | Admin | Delete a page from Pages section" @@@ fun _ ->        
        logOff()
        loginAsAdmin() |> ignore
        closePersonaBarIfOpen()
        pageName <- openNewPage false
        openPBPages()
        deletePage pageName

    "Recycle Bin | Admin | Restore a page from Recycle Bin" @@@ fun _ ->   
        restorePage pageName

    "Recycle Bin | Admin | Remove a page from Recycle Bin" @@@ fun _ ->
        reloadPage()
        openPBPages()
        deletePage pageName
        removePage pageName

    "Recycle Bin | Admin | Empty Recycle Bin" @@@ fun _ ->
        pageName <- openNewPage false
        openPBPages()
        deletePage pageName
        emptyRecycleBin()
        //verify page is deleted
        click "//li/a[@href='#pages']" //Pages tab of Recycle Bin
        waitForAjax()
        let pageDiv = sprintf "//div[.='%s']" pageName
        if existsAndVisible pageDiv then
            failwithf "  FAIL: Page %A is still visible after emptying Recycle Bin" pageName

let all _ =
    adminTests()      

