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

module DnnDAM

open canopy
open System.IO
open DnnAdmin

let createFolderDam foldername foldertype =
    scrollToOrigin()
    let rootSelector = "//div[contains(@class,'jspPane')]/div/ul/li/div/img"
    click rootSelector
    click "#DigitalAssetsCreateFolderBtnId"
    sleep 1
    let ctrId = extractControlId "//input[contains(@id,'_View_FolderNameTextBox')]"
    let folderNameTb = element ("#dnn_ctr" + ctrId + "_View_FolderNameTextBox")
    folderNameTb << foldername
    //folder type
    let ftype = getFolderTypeName(foldertype)
    enabled ("#dnn_ctr" + ctrId + "_View_FolderTypeComboBox_Arrow")
    click ("#dnn_ctr" + ctrId + "_View_FolderTypeComboBox_Arrow")
    sleep 0.2
    let rcbList = element ("#dnn_ctr" + ctrId + "_View_FolderTypeComboBox_DropDown") |> elementsWithin "li"
    let ftypeElement = rcbList |> List.tryFind (fun e -> e.Text = ftype)
    match ftypeElement with
    | None -> failwithf "  ERROR: couldn't find folder type %A in the Folder Types drop-down list" ftype
    | Some(someftype) -> click someftype
    click "#save_button"

let uploadFileDam fpath =
    scrollToOrigin() 
    let mutable success = false
    if File.Exists(fpath) then
        click "//button[@id='DigitalAssetsUploadFilesBtnId']"
        waitForAjax()
        let chooseFile = element "//input[@name='postfile']"  
        chooseFile << (fixfilePath fpath)
        waitForAjax()
        click "//button[contains(@class,'dnnSecondaryAction')]" //close button
        waitForAjax()
        if existsAndVisible fileSpanSelectorDam then
            success <- true  
    else
        failwithf "  ERROR: File does not exist: %A\n" fpath
    success
