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

module DnnSiteSettings

open canopy

/// <summary>Picks a dropdown item in persona bar</summary>
/// <param name="dropdown">Selector of the dropdown to be picked from</param>
/// <param name="itemName">Name of the item to pick</param>
let pickDdnItem dropdown itemName =
    click dropdown
    waitForAjax()
    let theItem = sprintf "//div[@class='item-name' and contains(.,\"%s\")]" itemName
    if existsAndVisible theItem then
        click theItem
    //else search for the item and click it

/// <summary>Verifies the current Site Logo</summary>
/// <param name="logoName">Name of the site logo image</param>
let verifySiteLogo logoName =
    let newLogoImg = 
        if exists "a#dnn_dnnLOGO_hypLogo" then sprintf "a#dnn_dnnLOGO_hypLogo>img[src*=\"%s\"]" logoName
        else sprintf "img[src*=\"%s\"]" logoName
    if not(existsAndVisible newLogoImg) then
        failwithf "  FAIL: Site Logo was not %A" logoName

/// <summary>Verifies the current favicon</summary>
/// <param name="faviconName">Name of the favicon</param>
let verifyFavicon faviconName =
    let faviconImg = sprintf "link[rel='SHORTCUT ICON'][href*=\"%s\"]" faviconName
    if not(exists faviconImg) then
        failwithf "  FAIL: Favicon was not %A" faviconName

/// <summary>Changes Site Logo or Favicon by uploading new image</summary>
/// <param name="name">Name of the file to upload</param>
/// <param name="path">Path of the file to upload</param>
/// <param name="uploadBtn">Input Selector of the upload button</param>
let private changeSiteImageNew name path uploadBtn = 
    openPBSiteSettings()
    click "div.siteSettings-app>div>div>div>div>ul>li:nth-child(1)"
    waitForAjax()
    scrollTo uploadBtn  
    uploadBtn << (fixfilePath path)
    sleep 0.5
    waitForElementPresent (sprintf "div.image-container>img[src*=\"%s\"]" name)
    click "div.siteSettings-Root>div>div>div>div>div>div>div>div.buttons-box>button[role=primary]"
    waitForAjax()

/// <summary>Changes the site logo image by uploading new file</summary>
/// <param name="logoName">The name of the site logo image</param>
/// <param name="imgPath">Path of the site log image</param>
let changeSiteLogoNew logoName imgPath = 
    changeSiteImageNew logoName imgPath "div.left-column>div>div>div>div#dropzoneId>div>div>input"

/// <summary>Changes the favicon by uploading new file</summary>
/// <param name="iconName">The name of the favicon</param>
/// <param name="iconPath">Path of the favicon</param>
let changeFaviconNew iconName iconPath =
    changeSiteImageNew iconName iconPath "div.right-column>div>div>div>div#dropzoneId>div>div>input"

/// <summary>Changes Site Logo or Favicon by choosing existing image</summary>
/// <param name="name">Name of the file</param>
/// <param name="folder">Folder of the file</param>
/// <param name="browseBtn">Selector of the browse button</param>
let private changeSiteImageExisting name folder browseBtn =
    openPBSiteSettings()
    click "div.siteSettings-app>div>div>div>div>ul>li:nth-child(1)"
    waitForAjax()
    scrollTo browseBtn
    click browseBtn
    waitForAjax()
    let folderDdn = "div.file-upload-container>div.drop-down:first-of-type"    
    pickDdnItem folderDdn folder
    let fileDdn = "div.file-upload-container>div.drop-down:last-of-type"
    pickDdnItem fileDdn name    
    click "div.file-upload-container>span>strong:first-of-type" //click enter link
    click "div.siteSettings-Root>div>div>div>div>div>div>div>div.buttons-box>button[role=primary]"
    waitForAjax() 

/// <summary>Changes the site logo image by selecting existing image</summary>
/// <param name="logoName">Name of the site logo image</param>
/// <param name="logoFolder">Folder of the site logo image</param>
let changeSiteLogoExisting logoName logoFolder =
    changeSiteImageExisting logoName logoFolder "div.left-column>div>div>div>div#dropzoneId>div>div.browse"

/// <summary>Changes the favicon by selecting existing icon</summary>
/// <param name="iconName">Name fo the favicon</param>
/// <param name="iconFolder">Folder of the favicon</param>
let changeFaviconExisting iconName iconFolder =
    changeSiteImageExisting iconName iconFolder "div.right-column>div>div>div>div#dropzoneId>div>div.browse"
