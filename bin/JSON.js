var parseJSON_stat = null;
function parseJSON(str)
{
    if (!parseJSON_stat) {
        parseJSON_stat = {
            "re_space":  new RegExp("^(\\s|(\\/\\/[^\\n]*\\n|\\/\\*(\\*[^\\/]|[^\\*])*\\*\\/))*"),
            "re_true":   new RegExp("^true\\b",  "i"),
            "re_false":  new RegExp("^false\\b", "i"),
            "re_null":   new RegExp("^null\\b",  "i"),
            "re_number": new RegExp("^[\\-\\+]?\\d+(\\.\\d+)?([Ee][\\-\\+]?\\d+)?"),
            "re_string": new RegExp("^(\\\"((\\\\\\\"|[^\\\"\\n])*)\\\"|\\\'((\\\\\\\'|[^\\\'\\n])*)\\\')"),
            "re_ident":  new RegExp("^(\\w+|\\\"((\\\\\\\"|[^\\\"\\n])*)\\\"|\\\'((\\\\\\\'|[^\\\'\\n])*)\\\')"),
            "re_escape": new RegExp("\\\\((u)([0-9A-Fa-f]{4,4})|[\\\\\\\\\"\\\'/bfnrt])", "g"),
            "func_unescape" : function($0, $1, $2, $3)
            {
                if ($1 == "\\") return "\\";
                if ($1 == "\"") return "\"";
                if ($1 == "\'") return "\'";	// Tolerate escaped apostrophes \'
                if ($1 == "/")  return "/";
                if ($1 == "b")  return "\b";
                if ($1 == "f")  return "\f";
                if ($1 == "n")  return "\n";
                if ($1 == "r")  return "\r";
                if ($1 == "t")  return "\t";
                if ($2 == "u")  return String.fromCharCode(parseInt($3, 16));
                return $0;
            }
        };
    }

    function getCodeSection(str)
    {
        return str.length < 200 ? str : str.substring(0, 199) + "…";
    }

    function parse()
    {
        var m;

        if (m = str.match(parseJSON_stat.re_true)) {
            str = str.substring(m.lastIndex);
            return true;
        }

        if (m = str.match(parseJSON_stat.re_false)) {
            str = str.substring(m.lastIndex);
            return false;
        }

        if (m = str.match(parseJSON_stat.re_null)) {
            str = str.substring(m.lastIndex);
            return null;
        }

        if (m = str.match(parseJSON_stat.re_number)) {
            str = str.substring(m.lastIndex);
            var x = new Number(m[0]).valueOf();
            var x_32 = x | 0;
            return x_32 == x ? x_32 : x;
        }

        if (m = str.match(parseJSON_stat.re_string)) {
            str = str.substring(m.lastIndex);
            return m[m[2].length ? 2 : 4].replace(parseJSON_stat.re_escape, parseJSON_stat.func_unescape);
        }

        var c = str.charAt(0);

        if (c == "[") {
            var str_tmp = getCodeSection(str);
            var obj = new Array();
            var is_empty = true;
            var has_separator = false;
            str = str.substring(1);
            while (str.length) {
                str = str.replace(parseJSON_stat.re_space, "");
                if (str.charAt(0) == "]") {
                    str = str.substring(1);
                    return obj;
                } else if (is_empty || has_separator) {
                    obj.push(parse());
                    is_empty = false;
                    str = str.replace(parseJSON_stat.re_space, "");
                    if (str.charAt(0) == ",") {
                        str = str.substring(1);
                        has_separator = true;
                    } else
                        has_separator = false;
                } else
                    throw new Error(4, "JSON Syntax Error: Comma \",\" or right bracket \"]\" expected before \"" + getCodeSection(str) + "\"");
            }
            throw new Error(4, "JSON Syntax Error: No matching right bracket \"]\" in \"" + str_tmp + "\"");
        }

        if (c == "{") {
            var str_tmp = getCodeSection(str);
            var obj = new Object();
            var is_empty = true;
            var has_separator = false;
            str = str.substring(1);
            while (str.length) {
                str = str.replace(parseJSON_stat.re_space, "");
                if (str.charAt(0) == "}") {
                    str = str.substring(1);
                    return obj;
                } else if (is_empty || has_separator) {
                    if (!(m = str.match(parseJSON_stat.re_ident)))
                        throw new Error(4, "JSON Syntax Error: Unknown element name \"" + getCodeSection(str) + "\"");
                    str = str.substring(m.lastIndex);
                    var name;
                         if (m[2].length) name = m[2].replace(parseJSON_stat.re_escape, parseJSON_stat.func_unescape);
                    else if (m[4].length) name = m[4].replace(parseJSON_stat.re_escape, parseJSON_stat.func_unescape);
                    else                  name = m[1];
                    if (name in obj)
                        throw new Error(4, "JSON Syntax Error: Duplicate element name \"" + name + "\"");
                    str = str.replace(parseJSON_stat.re_space, "");
                    if (str.charAt(0) != ":")
                        throw new Error(4, "JSON Syntax Error: Missing semicolon \":\" before \"" + getCodeSection(str) + "\"");
                    str = str.substring(1);
                    str = str.replace(parseJSON_stat.re_space, "");
                    obj[name] = parse();
                    is_empty = false;
                    str = str.replace(parseJSON_stat.re_space, "");
                    if (str.charAt(0) == ",") {
                        str = str.substring(1);
                        has_separator = true;
                    } else
                        has_separator = false;
                } else
                    throw new Error(4, "JSON Syntax Error: Comma \",\" or right bracket \"}\" expected before \"" + getCodeSection(str) + "\"");
            }
            throw new Error(4, "JSON Syntax Error: No matching right bracket \"}\" in \"" + str_tmp + "\"");
        }

        throw new Error(4, "JSON Syntax Error: Unknown value \"" + getCodeSection(str) + "\"");
    }

    str = str.replace(parseJSON_stat.re_space, "");
    var obj = parse();
    str = str.replace(parseJSON_stat.re_space, "");
    if (str.length)
        throw new Error(4, "JSON Syntax Error: Excessive trailing data \"" + getCodeSection(str) + "\"");
    return obj;
}
