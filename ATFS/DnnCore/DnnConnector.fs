module DnnConnector

open canopy

///conn: connectorsList: type of the connector
let openConnectorPB (conn:ConnectorsList) =
    let connectorName = getConnectorName conn
    let connectorNameUpperCase = connectorName.ToUpper()
    let mutable rownumber = 1
    let mutable connectorSelector = "//table[@id='connectionstbl']/tbody/tr[" + rownumber.ToString() + "]/td[3]/span"
    let mutable found = false
    while exists connectorSelector && not found do
        let readConnectorName = (element connectorSelector).Text.ToString()
        if readConnectorName = connectorNameUpperCase then  
            found <- true
        if not(found) then
            rownumber <- rownumber + 2
            connectorSelector <- "//table[@id='connectionstbl']/tbody/tr[" + rownumber.ToString() + "]/td[3]/span"
    if not(found) then
        failwithf "  FAIL: Connector %A not found" connectorName
    else
        let connectBtnSelector = "//table[@id='connectionstbl']/tbody/tr[" + rownumber.ToString() + "]/td[4]/a[.='Connect']"
        let editBtnSelector = "//table[@id='connectionstbl']/tbody/tr[" + rownumber.ToString() + "]/td[4]/a[.='Edit']"
        if existsAndVisible connectBtnSelector then 
            scrollTo connectBtnSelector
            click connectBtnSelector
        if existsAndVisible editBtnSelector then
            scrollTo editBtnSelector
            click editBtnSelector
        let editRowNumber = rownumber + 1
        let saveBtnSelector = "//table[@id='connectionstbl']/tbody/tr[" + editRowNumber.ToString() + "]/td/div/div[2]/div[2]/a"
        waitForElementPresent saveBtnSelector
    rownumber

/// <summary>Selects the last folder in the Connector's folder dropdown</summary>
let private selectConnFolder() =
    //need to wait for notif message to disappear to confirm process completed
    try
        waitForElementPresent "div#notification-dialog" //wait for notif msg
        waitForElement "div#notification-dialog[style*='display: none']" //wait for notif msg to disappear
    with _ -> ()
    //find the right dropdown
    let folderDdns = elements "div.pb-dropdown"
    let theFolderDdn = folderDdns |> List.find ((fun e -> existsAndVisible e))
    click theFolderDdn
    waitClick "div.pb-dropdown.open>div>ul>li:last-of-type"
    waitForAjax()

/// <summary>Selects the last folder in the Connector's folder dropdown - for multiple connectors</summary>
let private selectMultiConnFolder() =
    //need to wait for notif message to disappear to confirm process completed
    try
        waitForElementPresent "div#notification-dialog" //wait for notif msg
        waitForElement "div#notification-dialog[style*='display: none']" //wait for notif msg to disappear
    with _ -> ()    
    //find the right dropdown        
    let folderDdns = elements "div.full-row>select"
    let theFolderDdn = folderDdns |> List.find ((fun e -> existsAndVisible e))
    let lastOption = (theFolderDdn |> elementWithin "option:last-child").Text
    theFolderDdn << lastOption
    waitForAjax()  

/// <summary>Enters connection string values into Connector fields</summary>
/// <param name="conn">Type of the Connector</param>
/// <param name="inputIDSelector">Selector of the first input field</param>
/// <param name="inputPasswordSelector">Selector of the password field, if needed</param>
/// <param name="inputIDSelector2">Selector of the second input field, if needed</param>
/// <param name="saveBtn">Selector of the save buton</param>
let enterConnectorValues conn inputIDSelector inputPasswordSelector inputIDSelector2 saveBtn =
    match conn with
    | ConnectorsList.AZURE ->
        inputIDSelector << azureAccountName
        inputPasswordSelector << azureAccountKey
        click saveBtn
        waitForAjax()
        //choose a folder
        selectConnFolder()
    | _ -> failwithf "Unknown connector %A" conn
    scrollTo saveBtn
    click saveBtn
    waitForAjax()

/// <summary>Enters Multi connection string values into Connector fields</summary>
/// <param name="conn">Type of the Connector</param>
/// <param name="inputIDSelector">Selector of the first input field</param>
/// <param name="inputPasswordSelector">Selector of the password field, if needed</param>
/// <param name="inputIDSelector2">Selector of the second input field, if needed</param>
/// <param name="saveBtn">Selector of the save buton</param>
let enterMultiConnectorValues conn inputIDSelector inputPasswordSelector inputIDSelector2 saveBtn =
    match conn with
    | ConnectorsList.AZURE ->
        inputIDSelector << azureAccountName
        inputPasswordSelector << azureAccountKey
        click saveBtn
        waitForAjax()
        selectMultiConnFolder()
    | _ -> failwith "  Fail: This Connector does not support multiple connections"
    scrollTo saveBtn
    click saveBtn
    waitForAjax()

/// <summary>Entering Negative value for Connectors </summary>
/// <param name="conn">Type of the Connector</param>
/// <param name="inputIDSelector">Selector of the first input field</param>
/// <param name="inputPasswordSelector">Selector of the password field, if needed</param>
/// <param name="inputIDSelector2">Selector of the second input field, if needed</param>
/// <param name="saveBtn">Selector of the save buton</param>
let enterConnectorNegValues (conn:ConnectorsList) inputIDSelector inputPasswordSelector inputIDSelector2 saveBtn =
    inputIDSelector << "dsfdsfadsfads" + getRandomId()
    if conn <> ConnectorsList.UNC then
        inputPasswordSelector << "dsffaddasfdsafadsfsadf" + getRandomId()
    click saveBtn
    waitPageLoad() 

///conn: connectorsList: type of the connector
///removeconnectoraftertest: bool: After the test, remove the connector
let addConnectorPB (conn:ConnectorsList) (removeconnectoraftertest:bool) =
    let connectorName = getConnectorName conn
    let connectorSpanSelector = "//span[contains(.,\"" + connectorName + "\")]"
    if not(existsAndVisible connectorSpanSelector) then
        failwithf "  FAIL: %A Connector not found" connectorName
    //open connector and find row number
    let rowNumber = openConnectorPB conn
    let editRowNumber = rowNumber + 1
    let editRowSelector = "//table[@id='connectionstbl']/tbody/tr[" + editRowNumber.ToString() + "]"
    let mutable inputIDSelector = null
    let mutable inputIDSelector2 = null
    let mutable inputPasswordSelector = null
    let mutable iconSettingsSelector = null
    let mutable cancelBtn = ""
    let mutable saveBtn = ""
    //check that all the input fields exist
    inputIDSelector <- editRowSelector + "/td/div/div[1]/div/div[1]/input"
    if not(existsAndVisible inputIDSelector) then
        failwithf "  FAIL: Input field of type Text is missing for Connector %A" connectorName
    if conn <> ConnectorsList.UNC then
        inputPasswordSelector <- editRowSelector + "/td/div/div[1]/div/div[2]/input"
        if not(existsAndVisible inputPasswordSelector) then
            failwithf "  FAIL: Input field of type Password is missing for Connector %A" connectorName
    //check that cancel and save buttons exist
    cancelBtn <- editRowSelector + "/td/div/div[2]/div[1]/a"
    saveBtn <- editRowSelector + "/td/div/div[2]/div[2]/a"
    if not(existsAndVisible cancelBtn) || not(existsAndVisible saveBtn) then
        failwithf "  FAIL: Cancel/Save Buttons missing for Connector %A" connectorName  
    //enter values and save
    enterConnectorValues conn inputIDSelector inputPasswordSelector inputIDSelector2 saveBtn
    //verify
    let greenCheckSelector = "//table[@id='connectionstbl']/tbody/tr[" + rowNumber.ToString() + "]/td[1]/span[contains(@class,'verified')]"
    try
        waitForElementPresent greenCheckSelector
    with _ -> ()
    if not(existsAndVisible greenCheckSelector) then
        failwithf "  FAIL: Green Check not visible for Connector %A after verification" connectorName
    //let editBtnSelector = "//table[@id='connectionstbl']/tbody/tr[" + rowNumber.ToString() + "]/td[4]/a[.='Edit']"
    //if not(existsAndVisible editBtnSelector) then
    //    failwithf "  FAIL: Edit Button not visible for Connector %A after verification" connectorName
    let iconSelector = "//table[@id='connectionstbl']/tbody/tr[" + rowNumber.ToString() + "]/td[2]/span[contains(@class,'connected')]"
    if not(existsAndVisible iconSelector) then
        failwithf "  FAIL: Icon not visible for Connector %A after verification" connectorName
    if removeconnectoraftertest then
        //click editBtnSelector
        inputIDSelector << ""
        if conn <> ConnectorsList.UNC then
            inputPasswordSelector << "" 
        click saveBtn
        waitPageLoad() 

/// <summary>Negative testing for Connectors </summary>
/// <param connectorsList = type of the connector </param>
/// <param checkconnectorAfterTest = After the test, check the connector has empty value</param>
let negConnectorPB (conn:ConnectorsList) (checkConnectorAfterTest:bool) =
    let connectorName = getConnectorName conn
    let connectorSpanSelector = "//span[contains(.,\"" + connectorName + "\")]"
    if not(existsAndVisible connectorSpanSelector) then
        failwithf "  FAIL: %A Connector not found" connectorName
    //open connector and find row number
    let rowNumber = openConnectorPB conn
    let editRowNumber = rowNumber + 1
    let editRowSelector = "//table[@id='connectionstbl']/tbody/tr[" + editRowNumber.ToString() + "]"
    let mutable inputIDSelector = null
    let mutable inputIDSelector2 = null
    let mutable inputPasswordSelector = null
    let mutable iconSettingsSelector = null
    let mutable cancelBtn = ""
    let mutable saveBtn = ""
    //check that all the input fields exist
    inputIDSelector <- editRowSelector + "/td/div/div[1]/div/div[1]/input"
    if not(existsAndVisible inputIDSelector) then
        failwithf "  FAIL: Input field of type Text is missing for Connector %A" connectorName
    if conn <> ConnectorsList.UNC then
        inputPasswordSelector <- editRowSelector + "/td/div/div[1]/div/div[2]/input"
        if not(existsAndVisible inputPasswordSelector) then
            failwithf "  FAIL: Input field of type Password is missing for Connector %A" connectorName
    //check that cancel and save buttons exist
    cancelBtn <- editRowSelector + "/td/div/div[2]/div[1]/a"
    saveBtn <- editRowSelector + "/td/div/div[2]/div[2]/a"
    if not(existsAndVisible cancelBtn) || not(existsAndVisible saveBtn) then
        failwithf "  FAIL: Cancel/Save Buttons missing for Connector %A" connectorName  
    //enter values and save
    enterConnectorNegValues conn inputIDSelector inputPasswordSelector inputIDSelector2 saveBtn 
    //verify
    let greenCheckSelector = "//table[@id='connectionstbl']/tbody/tr[" + rowNumber.ToString() + "]/td[1]/span[contains(@class,'verified')]"
    if not(existsAndVisible greenCheckSelector) then
        click cancelBtn
    let iconSelector = "//table[@id='connectionstbl']/tbody/tr[" + rowNumber.ToString() + "]/td[2]/span[contains(@class,'connected')]"
    if not(existsAndVisible iconSelector) then
        printfn "  Icon not visible for Connector %A after verification" connectorName
    if checkConnectorAfterTest then
        openConnectorPB conn |> ignore
        let inputIDCssSelector =  getJavaScriptValue( ("$(\"" + sprintf "table#connectionstbl>tbody>tr:nth-of-type(%i)>td>div>div>div>div>input" editRowNumber + "\").val()") )
        if existsAndVisible inputIDCssSelector then
            if inputIDCssSelector <> "" then
                failwithf "  FAIL: Input value visible for Connector %A after verification" connectorName
    click cancelBtn
    waitPageLoad()

/// <summary>Adds a multi-level connector</summary>
/// <param name="conn">Type of the connector</param>
let addMultiConnectorPB (conn:ConnectorsList) = 
    //open connector and find row number
    let rowNumber = openConnectorPB conn
    let editRow = "//table[@id='connectionstbl']/tbody/tr[" + rowNumber.ToString() + "]"
    let addNewLink = editRow + "/td/a[contains(@class,'add-new-connection')]"
    if not(existsAndVisible addNewLink) then
        failwith "  FAIL: Add New link not visible"
    scrollToAboveElement addNewLink 100
    click addNewLink
    waitForAjax()
    let inputIDSelector = "div.sub-connector-row-edit.open>div>div.edit-fields>div>div:nth-of-type(1)>input"
    let inputPasswordSelector = "div.sub-connector-row-edit.open>div>div.edit-fields>div>div:nth-of-type(2)>input"
    let inputIDSelector2 = null
    let saveBtn = "div.sub-connector-row-edit.open>div>div.edit-buttons>div>a.primarybtn"
    //enter values and save
    enterMultiConnectorValues conn inputIDSelector inputPasswordSelector inputIDSelector2 saveBtn
    //verify
    let greenCheckSelector = "div.sub-connector.open>span.verified"
    try
        waitForElementPresent greenCheckSelector
    with _ -> ()
    if not(existsAndVisible greenCheckSelector) then
        failwith "  FAIL: Green Check not visible"
    let iconSelector = "div.sub-connector.open>span.connected"
    if not(existsAndVisible iconSelector) then
        failwith "  FAIL: Connected Icon not visible"

