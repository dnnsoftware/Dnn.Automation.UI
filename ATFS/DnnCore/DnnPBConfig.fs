module DnnPBConfig

let allProductsModuleList = 
    [
        "Users And Roles"; "Search Results"; "Account Login"; "ViewProfile"; "Account Registration";
        "Module Creator"; "DDR Menu"; "Message Center";  "Html Editor Management"; "Journal"; 
        "Member Directory"; "Razor Host"; "Social Groups"; "DotNetNuke Client Capability Provider"; "Console"
    ]

let platformModuleList = List.append allProductsModuleList [ "Digital Asset Management"; "HTML" ]
