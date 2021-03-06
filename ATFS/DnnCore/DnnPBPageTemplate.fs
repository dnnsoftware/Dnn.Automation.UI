﻿// Automation UI Testing Framework - ATFS - http://www.dnnsoftware.com
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

module DnnPBPageTemplate

open canopy

/// <summary>Edit the Page Template</summary>
/// <param name="pageTemplateName">The Name of the Page Template</param>
let openTemplateEditMode pageTemplateName =
    let pageTemp= sprintf "//span[@class='subtitle field-name' and .='%s']" pageTemplateName
    hoverOver pageTemp
    let editIcon = sprintf "//span[@class='subtitle field-name' and .='%s']/../span[@class='actions']/span[@class='buttons']/span[@class='edit-page']" pageTemplateName
    waitForElementPresent editIcon
    click editIcon  
    waitForElement "//div[@id='edit-bar']"

/// <summary>Delete the Page Template</summary>
/// <param name="pageTemplateName">The Name of the Page Template</param>
let deletePageTemplate pageTemplateName =
    let pageTemp= sprintf "//span[@class='subtitle field-name' and .='%s']"  pageTemplateName
    hoverOver pageTemp
    let deleteIcon = sprintf "//span[@class='subtitle field-name' and .='%s']/../span[@class='actions']/span[@class='buttons']/span[@class='delete-page']" pageTemplateName
    waitForElementPresent deleteIcon
    click deleteIcon
    waitForElementPresent "//div[@id='confirmation-dialog']"
    click "//a[@id='confirmbtn']"
    waitPageLoad()

/// <summary> verify edit  and Delete Icon for the Page Template</summary>
/// <param name="pageTemplateName">The Name of the Page Template</param>
let verifyPageTemplateUIIcons pageTemplateName =  
    let pageTemp= sprintf "//span[@class='subtitle field-name' and .='%s']" pageTemplateName
    waitForElement pageTemp 
    hoverOver pageTemp
    let editIcon = sprintf "//span[@class='subtitle field-name' and .='%s']/../span[@class='actions']/span[@class='buttons']/span[@class='edit-page']" pageTemplateName
    if not (existsAndVisible editIcon ) then failwithf "%s' Page template cannot be edit" pageTemplateName  
    let deleteIcon = sprintf "//span[@class='subtitle field-name' and .='%s']/../span[@class='actions']/span[@class='buttons']/span[@class='delete-page']" pageTemplateName
    if not (existsAndVisible deleteIcon ) then failwithf "%s' Page template cannot be edit" pageTemplateName

/// <summary>Search the Page Template</summary>
/// <param name="pageTemplateName">The Name of the Page Template</param>
let searchPageTemplate pageTemplateName =
    let searchInput = "//input[@class='personaBar-input search']"
    searchInput << pageTemplateName
    let pageTempName = sprintf "//span[@class='subtitle field-name' and .='%s']" pageTemplateName
    waitForAjax()
    let r = existsAndVisible pageTempName
    (r)

/// <summary>verify the Large Thumbnail for Page Template</summary>
/// <param name="pageTemplateName">The Name of the Page Template</param>
let verifyLargeThumbPageTemplate pageTemplateName =
    let pageThumbnail= "//div[@class='page-item']/span[@class='thumbnail']"
    hoverOver pageThumbnail
    let largeThumbnail = "//div[@class='pages-preview top']/img"
    waitForElement largeThumbnail
    if not (existsAndVisible largeThumbnail) then failwithf "'%s' Page template thumbnail exist" pageTemplateName
    reloadPage()

/// <summary>Creates a Page Template </summary>
/// <param name="pageTemplateName">The Name of the Page Template</param>
/// <param name="pageTemplateDesc">The description of the Page Template</param>
/// <param name="pageTemplateSelect">The template need to be  selected create a page Template </param>
let createPageTemplate name desc select  =
    click "//button[@class='dnn-ui-common-button large add-template-button']"        
    let inputTemplateName = "//div[@class='form-item short short-left']/input[@type='text']"
    inputTemplateName << name
    let inputTemplateDesc = "//div[@class='form-item']/textarea[@type='text']"
    inputTemplateDesc << desc
    let defaultTemplate = "Cavalier - Main"
    if select <> defaultTemplate then
        let selectPageTemplate = sprintf "//span[@class='name' and .='%s']" select
        click selectPageTemplate
    click "//a[@class='button create-page primarybtn']"
    //waitForAjax()
    reloadPage()
    let pageTempName = sprintf "//span[@class='subtitle field-name' and .='%s']" name
    waitForElement pageTempName
    if not (existsAndVisible pageTempName) then failwithf " '%s' template is not get created" pageTempName
    waitForAjax()
    name