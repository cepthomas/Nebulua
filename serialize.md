
# Types

|C# type     |Lua type                |json type                                                                |
|------------|------------------------|-----------------------------                                            |
|null        |Nil                     |null                                                                     |
|string      |String                  |string                                                                   |
|bool        |Boolean                 |boolean                                                                  |
|int         |Number (IsInteger())    |number                                                                   |
|dict? Bag?  |Table                   |object  { "employee":{"name":"John", "age":30, "city":"New York"} }      |
|List        |N/A                     |array  { "employees":["John", "Anna", "Peter"] }                         |
|Func<T>     |Function                |string for func name?                                                    |

Thread, UserData, LightUserData are lua internal only.



# C# -> Lua
ret_type func(arg_type, ...);


TODO support include="Test1_DataDif"?


# Like dif
```json
{
    "enum":
    {
        "name":"StatusType_Enum",
        "value":
        [
            { "name":"Ready", "number":"1" },
            { "name":"InProcess", "number":"2" },
            { "name":"Done", "number":"3" }
        ]
    }
}

{
    "data":
    {
        "name":"ProgramTest",
        "properties":
        [
            { "name":"Flag", "type":"Bool", "qualifier":"" },
            { "name":"When", "type":"DateTime", "qualifier":"" },
            { "name":"Status", "type":"StatusType_Enum", "qualifier":"" },
            { "name":"Params", "type":"Double", "qualifier":"array" }
        ]
    }
}

{
    "data":
    {
        "name":"Program",
        "properties":
        [
            { "name":"Flag", "type":"Bool", "qualifier":"" },
            { "name":"When", "type":"DateTime", "qualifier":"" },
            { "name":"Status", "type":"StatusType_Enum", "qualifier":"" },
            { "name":"Params", "type":"Double", "qualifier":"array" },
            { "name":"PatientId", "Type":"String", "qualifier":"" },
            { "name":"SampleId", "Type":"Int", "qualifier":"" },
            { "name":"Status", "Type":"StatusType_Enum", "qualifier":"" },
            { "name":"StatusList", "Type":"StatusType_Enum", "qualifier":"" },
            { "name":"Complete", "Type":"DateTime", "qualifier":"" },
            { "name":"TestUno", "Type":"ProgramTest", "qualifier":"" },
            { "name":"TestList", "Type":"ProgramTest", "qualifier":"" },
        ]
    }
}
```

