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
