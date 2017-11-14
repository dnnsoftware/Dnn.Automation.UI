module DnnRoles

open System
open canopy

/// <summary>Changes the Groups Filter in PB Roles</summary>
/// <param name="groupName">The name of the role group</param>
let changeGroupsFilter groupName =
    let filterDdn = "div.groups-filter"
    let filterChosen = sprintf "//div[@class='groups-filter']/div/div/div[@class='group-actions' and contains(.,\"%s\")]" groupName
    let doChangeFilter()=
        try
            waitForAjax()
            scrollToOrigin()
            let filterDdnInput = filterDdn + ">div>input"
            click filterDdn
            filterDdnInput << groupName
            let mutable filterSelected = sprintf "//div[@class='groups-filter']/div/div/div/div/div/div/ul/li[.=\"%s\" and @class='selected']" groupName
            try
                waitForElementPresent filterSelected
            with _ ->
                filterSelected <- sprintf "//div[@class='groups-filter']/div/div/div/div/ul/li[.=\"%s\" and @class='selected']" groupName
                waitForElementPresent filterSelected
            click filterSelected            
            waitForElementPresent filterChosen
            waitLoadingBar()
            true
        with _ ->
            reloadPage()
            waitForElementPresent filterDdn
            false
    if not(existsAndVisible filterChosen) then
        let changeSucess = retryWithWait 3 0.5 doChangeFilter
        if not changeSucess then
            failwithf "  FAIL: Roles Group %A could not be changed to successfully." groupName

/// <summary>Verifies whether a role exists within specified role group. Search performed if needed.</summary>
/// <param name="roleName">Name of the role</param>
/// <param name="groupName">Name of the role group. Null if its a global role.</param>
let verifyRoleAndGroupExist roleName groupName =
    click "div.groups-filter"
    let allGroups = 
        if existsAndVisible("div.groups-filter>div>div>div>div>div>div>ul>li:first-of-type") then
            "div.groups-filter>div>div>div>div>div>div>ul>li:first-of-type"
        else
            "div.groups-filter>div>div>div>div>ul>li:first-of-type"
    waitForElementPresent allGroups
    click allGroups
    waitForAjax()
    let loadMoreBtn = "div.loadMore>a"
    if existsAndVisible loadMoreBtn then 
        click loadMoreBtn
        waitForAjax()
    let roleRow = sprintf "//div[contains(@id,'roleRow')]/div/div[.=\"%s\"]" roleName
    if not(existsAndVisible roleRow) then
        "div.search-filter>div>input" << roleName
        waitForAjax()
        let searchResults = "//div[@id='users-header-row']/../div[contains(@class,'collapsible-component1')]"
        waitForElementPresent searchResults
    if not(existsAndVisible roleRow) then
        failwithf "  FAIL: Role %A was not found" roleName
    else
        let readGroup = sprintf "//div[.=\"%s\"]/../div[2]" roleName
        let readGroupName = (element readGroup).Text
        if readGroupName <> groupName then
            failwithf "  FAIL: Expected RoleGroup for Role %A was %A, but actually was %A." roleName groupName readGroupName

/// <summary>Creates a Role Group</summary>
/// <param name="groupName">Name of the Role Group</param>
let createRoleGroup groupName =
    let roleGroupDdn = "div.editor-container.right-column>div:first-of-type>div.dnn-dropdown"
    let newGroupLink = 
        if existsAndVisible (roleGroupDdn + ">div>div>div>div>div>ul>li>span.do-not-close") then
            roleGroupDdn + ">div>div>div>div>div>ul>li>span.do-not-close"
        else
            roleGroupDdn + ">div>div>div>ul>li>span.do-not-close"   
    click newGroupLink
    let nameTb = "div.role-group-editor>div>div>div>div>div>input"
    waitForElementPresent nameTb
    nameTb << groupName
    click "div.role-group-editor>div>div.actions>button[role=primary]"
    waitForAjax()
    let selectedRG = sprintf "//div[contains(@class,'dnn-dropdown')]/div[.=\"%s\"]" groupName
    waitForElementPresent selectedRG

/// <summary>Creates a new role</summary>
/// <param name="roleName">Name of the new role</param>
/// <param name="roleGroupName">Name of the role group. Null will make it a global role. Group will be created if it does not exist.</param>
let createNewRole roleName roleGroupName =
    click createRoleBtn
    waitForAjax()
    let roleNameTb = "div.role-details-editor>div>div>div>div>div>div>input"
    waitForElementPresent roleNameTb
    roleNameTb << roleName
    if not(String.IsNullOrEmpty(roleGroupName)) then
        let roleGroupDdn = "div.editor-container.right-column>div:first-of-type>div.dnn-dropdown"
        click roleGroupDdn
        let rgItem = sprintf "//li[.=\"%s\"]" roleGroupName
        if existsAndVisible rgItem then
            click rgItem
            waitForAjax()
        else
            createRoleGroup roleGroupName
    click "div.role-details-editor>div.buttons-box>button[role=primary]"
    waitForAjax()
    sleep 0.5
    verifyRoleAndGroupExist roleName roleGroupName

/// <summary>Edits a Role Group</summary>
/// <param name="roleGroupName">Name of the role group to be edited</param>
/// <param name="newName">New name of the role</param>
let editRoleGroup roleGroupName newName =
    changeGroupsFilter roleGroupName
    click "div.groups-filter>div>div>div>div.role-group-actions>a>*" //pencil icon
    let groupNameTb = "div.role-group-editor>div>div>div.form-item>div>div>input"
    let oldNameInTb = groupNameTb + sprintf "[value=%s]" roleGroupName
    waitForElementPresent oldNameInTb 
    sleep 0.5 //wait for old name to be bound to Group Name textbox
    groupNameTb << newName
    click "div.role-group-editor>div.edit-form>div.actions>button[role=primary]" //save btn
    waitForAjax()
    let newFilter = sprintf "//div[@class='groups-filter']/div/div/div[@class='group-actions' and contains(.,\"%s\")]" newName
    waitForElementPresent newFilter

/// <summary>Edits a Role</summary>
/// <param name="roleName">Name of the role to be edited</param>
/// <param name="groupName">Name of the role group of the role</param>
/// <param name="newName">New name of the role</param>
let editRole roleName groupName newName =
    changeGroupsFilter groupName
    let editPencil = sprintf "//div[.=\"%s\"]/../div[5]/a[@title='Edit Role']/*" roleName
    click editPencil
    let roleNameInput = "div.role-details-editor>div>div>div>div.editor-row>div>div.input-tooltip-container>input"
    waitForElementPresent roleNameInput
    roleNameInput << newName
    click "div.role-details-editor>div.buttons-box>button[role=primary]"
    waitForAjax()
    //need to wait for notif message to disappear to confirm process completed
    try
        waitForElementPresent "div#notification-dialog" //wait for notif msg
        waitForElement "div#notification-dialog[style*='display: none']" //wait for notif msg to disappear
    with _ -> ()

/// <summary>Deletes a role group. Group should have no roles.</summary>
/// <param name="groupName">Name of the role group to be deleted</param>
let deleteRoleGroup groupName =
    changeGroupsFilter groupName
    let trashCan = "div.groups-filter>div>div>div.group-actions>div>a:nth-child(2)>*"
    if not(existsAndVisible trashCan) then
        failwithf "  FAIL: Role Group %A is not deleteable because delete icon is not visible." groupName
    click trashCan
    let confirmBtn = "a#confirmbtn"
    waitForElementPresent confirmBtn
    click confirmBtn
    waitForAjax()
    let filterChosen = sprintf "//div[@class='groups-filter']/div/div/div[@class='group-actions' and contains(.,\"%s\")]" groupName
    if existsAndVisible filterChosen then
        failwithf "  FAIL: Role Group %A was not deleted." groupName

/// <summary>Deletes a role</summary>
/// <param name="roleName">Name of the role to be deleted</param>
/// <param name="groupName">Name of the role group of the role</param>
let deleteRole roleName groupName =
    changeGroupsFilter groupName
    let roleDiv = sprintf "//div[.=\"%s\"]" roleName
    let editPencil = roleDiv + "/../div[5]/a[@title='Edit Role']/*"
    click editPencil
    let deleteBtn = "div.role-details-editor>div.buttons-box>button:first-of-type"
    waitForElementPresent deleteBtn
    click deleteBtn
    let confirmBtn = "a#confirmbtn"
    waitForElementPresent confirmBtn
    click confirmBtn
    waitForAjax()
    if existsAndVisible roleDiv then
        failwithf "  FAIL: Role %A of Group %A was not be deleted." roleName groupName
