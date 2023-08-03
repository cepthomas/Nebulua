using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace KeraLuaEx
{
    /// <summary>C# representation of a lua table.
    /// Intended for carrying data only. Supported value types: 
    /// LuaType             C# Type
    /// -----------------   ---------------
    /// LuaType.Nil         null
    /// LuaType.String      string
    /// LuaType.Boolean     bool
    /// LuaType.Number      long or double
    /// LuaType.Table       List or Dictionary
    ///
    /// Lua tables support both array and map types. To be considered an array:
    ///  - all keys must be integers and not sparse.
    ///  - all values must be the same type.
    /// To be considered a map:
    ///  - all keys must be strings and unique.
    ///  - values can be any supported type.
    /// </summary>
    public class Table
    {
        #region Types
        /// <summary>What am I?</summary>
        public enum TableType { Unknown, List, Dictionary, Invalid }

        /// <summary>Representation of a lua table field.</summary>
        record TableField(object Key, object Value);
        #endregion

        #region Fields
        /// <summary>The collection of fields.</summary>
        readonly List<TableField> _tableFields = new();

        /// <summary>Ensure list homogenity.</summary>
        Type? _listValueType = null;
        #endregion

        #region Properties
        /// <summary>Representation of a lua table.</summary>
        public TableType Type { get; private set; } = TableType.Unknown;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default empty constructor.
        /// </summary>
        public Table()
        {
        }

        /// <summary>
        /// Construct from a list.
        /// </summary>
        /// <param name="vals"></param>
        public Table(List<object> vals)
        {
            foreach (var v in vals)
            {
                _tableFields.Add(new(vals.Count + 1, v));
            }

            Type = TableType.List;
        }

        /// <summary>
        /// Construct from a dictionary.
        /// </summary>
        /// <param name="vals"></param>
        public Table(Dictionary<string, object> vals)
        {
            foreach (var kv in vals)
            {
                _tableFields.Add(new(kv.Key, kv.Value));
            }

            Type = TableType.Dictionary;
        }

        /// <summary>
        /// Get a table from the lua stack.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Table GetTable(Lua l)
        {
            Table table = new();

            // Put a nil key on stack.
            l.PushNil();

            // Key(-1) is replaced by the next key(-1) in table(-2).
            while (l.Next(-2))
            {
                // Get key(-2) info.
                LuaType keyType = l.Type(-2)!;
                //string skey = l.ToString(-2)!;

                object? key = keyType switch
                {
                    LuaType.String => l.ToString(-2),
                    LuaType.Number => DetermineNumber(l, -2),
                    _ => throw new ArgumentException($"Unsupported key type {keyType} for {l.ToString(-2)}")
                };

                // Get type of value(-1).
                LuaType valType = l.Type(-1)!;

                object? val = valType switch
                {
                   LuaType.Nil => null,
                   LuaType.String => l.ToString(-1),
                   LuaType.Number => DetermineNumber(l, -1),//l.IsInteger(-1) ? l.ToInteger(-1) : l.ToNumber(-1),
                   LuaType.Boolean => l.ToBoolean(-1),
                   LuaType.Table => GetTable(l), // recursion!
                   _ => null // ignore others TODO2 or arg option?
                };

                if (val is not null)
                {
                    table.AddVal(key!, val);
                }

                // Remove value(-1), now key on top at(-1).
                l.Pop(1);
            }

            static object? DetermineNumber(Lua l, int index)
            {
                //return l.IsInteger(index) ? l.ToInteger(index) : l.ToNumber(index); //TODO3 ternary op doesn't work!?
                if (l.IsInteger(index)) { return l.ToInteger(index); }
                else { return l.ToNumber(index); }
            }

            return table;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Push onto lua stack.
        /// </summary>
        public void Push()//TODO1
        {
            ///// <summary>
            ///// Push a list of ints onto the stack (as C# function return).
            ///// </summary>
            ///// <param name="l"></param>
            ///// <param name="ints"></param>
            //public static void PushList(Lua l, List<int> ints) // overloads for doubles, strings
            //{
            //    //https://stackoverflow.com/a/18487635
            //    l.NewTable();
            //    for (int i = 0; i < ints.Count; i++)
            //    {
            //        l.NewTable();
            //        l.PushInteger(i + 1);
            //        l.RawSetInteger(-2, 1);
            //        l.PushInteger(ints[i]);
            //        l.RawSetInteger(-2, 2);
            //        l.RawSetInteger(-2, i + 1);
            //    }
            //}
            // typedef struct Point { int x, y; } Point;
            // static int returnImageProxy(lua_State *L)
            // {
            //     Point points[3] = {{11, 12}, {21, 22}, {31, 32}};
            //     lua_newtable(L);
            //     for (int i = 0; i < 3; i++) {
            //         lua_newtable(L);
            //         lua_pushnumber(L, points[i].x);
            //         lua_rawseti(L, -2, 1);
            //         lua_pushnumber(L, points[i].y);
            //         lua_rawseti(L, -2, 2);
            //         lua_rawseti(L, -2, i+1);
            //     }
            //     return 1;   // I want to return a Lua table like :{{11, 12}, {21, 22}, {31, 32}}
            // }
        }

        /// <summary>
        /// Add a value to the table. Checks consistency on the fly.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void AddVal(object key, object val)
        {
            string serr = "";

            switch (Type)
            {
                case TableType.Unknown:
                    // New table. Determine type from key.
                    switch (key)
                    {
                        case string _:
                            Type = TableType.Dictionary;
                            _tableFields.Add(new(key, val));
                            break;
                        case long _:
                            Type = TableType.List;
                            _listValueType = val.GetType();
                            _tableFields.Add(new(key, val));
                            break;
                        default:
                            serr = $"Invalid key type {key.GetType()}";
                            break;
                    }
                    break;

                case TableType.List:
                    if (key.GetType() == typeof(long) && val.GetType() == _listValueType)
                    {
                        _tableFields.Add(new(key, val));
                    }
                    else
                    {
                        serr = $"Mismatched table key type:{key.GetType()}";//TODO1 needs more info - table name
                    }
                    break;

                case TableType.Dictionary:
                    if (key.GetType() == typeof(string))
                    {
                        _tableFields.Add(new(key, val));
                    }
                    else
                    {
                        serr = $"Mismatched table key type:{key.GetType()} for key:{key}";//TODO1 needs more info - table name
                    }
                    break;

                case TableType.Invalid:
                    serr = $"Invalid table";
                    break;
            }

            if (serr.Length > 0)
            {
                Type = TableType.Invalid;
                throw new ArgumentException(serr);
            }
        }

        /// <summary>
        /// Return a list representing the lua table.
        /// </summary>
        /// <returns></returns>
        public List<object> AsList()//TODO1 test
        {
            // Convert and return.
            List<object> ret = new();

            if (Type == TableType.List)
            {
                foreach (var f in _tableFields)
                {
                    ret.Add(f.Value);
                }
            }
            else
            {
                throw new ArgumentException($"This is not a list table");
            }

            return ret;
        }

        /// <summary>
        /// Return a dict representing the lua table.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> AsDict()//TODO1 test
        {
            // Clone and return.
            Dictionary<string, object> ret = new();

            if (Type == TableType.Dictionary)
            {
                _tableFields.ForEach(f => ret[f.Key.ToString()!] = f.Value);
            }
            else
            {
                throw new ArgumentException($"This is not a dictionary table");
            }

            return ret;
        }

        /// <summary>
        /// Dump the table into a readable form.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public string Format(string tableName, int indent = 0)
        {
            List<string> ls = new();
           // string sindent2 = new(' ', 4 * (indent + 1));

            switch (Type)
            {
                case TableType.List:
                    List<object> lvals = new();
                    _tableFields.ForEach(f => lvals.Add(f.Value));
                    ls.Add($"{Indent(indent)}{tableName}(array):[ {string.Join(", ", lvals)} ]");
                    break;

                case TableType.Dictionary:
                    ls.Add($"{Indent(indent)}{tableName}(dict):");
                    indent += 1;

                    foreach (var f in _tableFields)
                    {
                        switch (f.Value)
                        {
                            case null:     ls.Add($"{Indent(indent)}{f.Key}(null):");      break;
                            case string s: ls.Add($"{Indent(indent)}{f.Key}(string):{s}"); break;
                            case bool b:   ls.Add($"{Indent(indent)}{f.Key}(bool):{b}");   break;
                            case int i:    ls.Add($"{Indent(indent)}{f.Key}(int):{i}");    break;
                            case long l:   ls.Add($"{Indent(indent)}{f.Key}(long):{l}");   break;
                            case double d: ls.Add($"{Indent(indent)}{f.Key}(double):{d}"); break;
                            case Table t:
                                //ls.Add($"{Indent(indent)}{key.ToString()}(dict):");
                                ls.Add($"{t.Format($"{f.Key}", indent)}");
                                break; // recursion!
                            default: throw new ArgumentException($"Unsupported type {f.Value.GetType()} for {f.Key}"); // should never happen
                        }
                    }
                    break;

                case TableType.Unknown:
                case TableType.Invalid:
                    ls.Add($"Table is {Type}");
                    break;
            }

            static string Indent(int indent)
            {
                return indent > 0 ? new(' ', 4 * indent) : "";
            }

            return string.Join(Environment.NewLine, ls);
        }

        /// <summary>
        /// Create a table from json.
        /// </summary>
        /// <param name="sjson">Json string</param>
        /// <returns>New Table</returns>
        public static Table FromJson(string sjson)
        {
            Table table = new();

            // Uses Utf8JsonReader directly.

            var options = new JsonReaderOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };
            var bytes = Encoding.ASCII.GetBytes(sjson);
            var reader = new Utf8JsonReader(bytes, options);
            while (reader.Read())
            {
                //Debug.Write(reader.GetString());
                Debug.WriteLine($"{reader.TokenType}:{reader.TokenStartIndex}");

                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        var str = reader.GetString();
                        Debug.WriteLine($"PropertyName({reader.TokenStartIndex}):{str}");
                        break;

                    case JsonTokenType.String:
                        str = reader.GetString();
                        Debug.WriteLine($"String({reader.TokenStartIndex}):{str}");
                        //Console.Write(text);
                        break;

                    case JsonTokenType.Number:
                        if (reader.TryGetInt64(out long value))
                        {
                            Debug.WriteLine($"Long({reader.TokenStartIndex}):{value}");
                        }
                        else
                        {
                            double dblValue = reader.GetDouble();
                            Debug.WriteLine($"Double({reader.TokenStartIndex}):{dblValue}");
                        }
                        break;

                        // etc....
                        // None = 0,
                        //    There is no value (as distinct from System.Text.Json.JsonTokenType.Null). This is the default token type if no data has been read.
                        // StartObject = 1,
                        //    The token type is the start of a JSON object.
                        // EndObject = 2,
                        //    The token type is the end of a JSON object.
                        // StartArray = 3,
                        //    The token type is the start of a JSON array.
                        // EndArray = 4,
                        //    The token type is the end of a JSON array.
                        // PropertyName = 5,
                        //    The token type is a JSON property name.
                        // Comment = 6,
                        //    The token type is a comment string.
                        // String = 7,
                        //    The token type is a JSON string.
                        // Number = 8,
                        //    The token type is a JSON number.
                        // True = 9,
                        //    The token type is the JSON literal true.
                        // False = 10,
                        //    The token type is the JSON literal false.
                        // Null = 11
                        //    The token type is the JSON literal null.
                }
                //Console.WriteLine();
            }

            return table;
        }

        /// <summary>
        /// Create json from table.
        /// </summary>
        /// <returns>Json string</returns>
        public string ToJson()
        {
            return "TODO2";
        }        
        #endregion
    }
}