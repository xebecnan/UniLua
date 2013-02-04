local cs = require "ffi.cs"

local CONSTRUCTOR   = "_New"
local TYPE_ACCESS   = "_Type"
local CONVERT_FROM  = "_ConvertFrom"

----------------------------------------------------------------------

local type_alias = {
    bool    = "Boolean",
    char    = "Char",
    byte    = "Byte",
    sbyte   = "SByte",
    short   = "Int16",
    ushort  = "UInt16",
    int     = "Int32",
    uint    = "UInt32",
    long    = "Int64",
    ulong   = "UInt64",
    float   = "Single",
    double  = "Double",
    decimal = "Decimal",
    string  = "String",
    object  = "Object",
}
local function get_type(typename)
    return cs.get_type(type_alias[typename] or typename)
end

local function typename_to_type(typename)
    local t = get_type(typename)
    if not t then
        error(">>>>>>>>>>>>>>>> typename_to_type unknown typename:" ..
            tostring(typename))
    end
    return t
end

local function parse_signature(signature)
    -- print("parse_signature:", signature)
    local ret, fname, partypes = cs.parse_signature(signature)
    -- print("ret:", ret)
    -- print("fname:", fname)
    -- print("partypes:", partypes)
    local params
    if partypes then
        params = {}
        for i, pname in ipairs(partypes) do
            params[i] = typename_to_type(pname)
        end
    end
    return {
        ret_typename = ret,
        fname = fname,
        partypes = params,
    }
    -- local s = 1
    -- local types = {}
    -- while true do
    --     local e = string.find(signature, ",", s)
    --     if e then
    --         local typename = string.sub(signature, s, e-1)
    --         local t = typename_to_type(typename)
    --         table.insert(types, t)
    --         s = e + 1
    --     else
    --         local typename = string.sub(signature, s)
    --         if #typename > 0 then
    --             local t = typename_to_type(typename)
    --             table.insert(types, t)
    --         end
    --         break
    --     end
    -- end
    -- return types
end

----------------------------------------------------------------------

local function constructor(self, signature)
    table.insert(self.__constructor, signature)
    return self
end

local function method(self, signature)
    table.insert(self.__methods, signature)
    return self
end

local function static_method(self, signature)
    table.insert(self.__static_methods, signature)
    return self
end

local function field(self, signature)
    table.insert(self.__fields, signature)
    return self
end

local function property(self, signature)
    table.insert(self.__properties, signature)
    return self
end

local function static_property(self, signature)
    table.insert(self.__static_properties, signature)
    return self
end

----------------------------------------------------------------------

local function new_class_mgr()
    local all_classes = {}

    local function declare_class(clsname)
        all_classes[clsname] = {
            -- cls = {},
            cls_methods  = {},
            cls_fields   = {},
            inst_methods = {},
            inst_fields  = {},
        }
    end

    local function is_class_declared(clsname)
        return all_classes[clsname] ~= nil
    end

    local function define_class(clsname)
        local clsinfo = all_classes[clsname]
        -- local cls           = clsinfo.cls
        local cls_methods   = clsinfo.cls_methods
        local cls_fields    = clsinfo.cls_fields
        local inst_methods  = clsinfo.inst_methods
        local inst_fields   = clsinfo.inst_fields

        local function add_inst_method(name, mtd)
            inst_methods[name] = mtd
        end

        local function add_inst_field(field_name, index, newindex)
            inst_fields[field_name] = {index, newindex}
        end
    
        local function add_class_method(name, mtd)
            -- cls[name] = mtd
            cls_methods[name] = mtd
        end

        local function add_class_field(field_name, index, newindex)
            cls_fields[field_name] = {index, newindex}
        end

        local mt = {
            __index = {
                add_inst_method = add_inst_method,
                add_inst_field  = add_inst_field,
                add_class_method = add_class_method,
                add_class_field  = add_class_field,
            },
        }
        return setmetatable({}, mt)
    end

    local function make_class(clsname)
        local clsinfo = all_classes[clsname]
        -- local cls = clsinfo.cls
        local cls_methods = clsinfo.cls_methods
        local cls_fields = clsinfo.cls_fields
        local cls_mt = {
            __index = function(self, key)
                if cls_fields[key] then
                    return cls_fields[key][1](self, key)
                elseif cls_methods[key] then
                    return cls_methods[key]
                end
            end,
            -- __index = cls,
            __newindex = function(self, key, value)
                if cls_fields[key] then
                    return cls_fields[key][2](self, key, value)
                end
            end,
        }
        return setmetatable({}, cls_mt)
    end

    local function make_instance(clsname, this)
        local clsinfo = all_classes[clsname]
        local inst_methods = clsinfo.inst_methods
        local inst_fields  = clsinfo.inst_fields
        local inst_mt = {
            __index = function(self, key)
                if inst_fields[key] then
                    return inst_fields[key][1](self, key)
                elseif inst_methods[key] then
                    return inst_methods[key]
                end
            end,
            __newindex = function(self, key, value)
                if inst_fields[key] then
                    return inst_fields[key][2](self, key, value)
                end
            end,
        }
        return setmetatable({
            __this = this,
        }, inst_mt)
    end

    local mt = {
        __index = {
            declare_class = declare_class,
            is_class_declared = is_class_declared,
            define_class = define_class,
            make_class = make_class,
            make_instance = make_instance,
        },
    }

    return setmetatable({}, mt)
end

----------------------------------------------------------------------

local function build_class(cls_mgr, self)
    local clsname = self.__class_name
    local def = cls_mgr.define_class(clsname)

    local function wrap_retval(retval, clsname)
        if cls_mgr.is_class_declared(clsname) then
            return cls_mgr.make_instance(clsname, retval)
        else
            return retval
        end
    end

    local function unwrap_param(val, clsname)
        if cls_mgr.is_class_declared(clsname) then
            return rawget(val, "__this")
        else
            return val
        end
    end

    local type_info = assert(get_type(clsname), clsname)

    def.add_class_method(TYPE_ACCESS, function() return type_info end)

    def.add_class_method(CONVERT_FROM, function(self)
        local this = rawget(self, "__this")
        return cls_mgr.make_instance(clsname, this)
    end)

    ----------------
    -- CONSTRUCTOR
    ----------------
    for _, signature in ipairs(self.__constructor) do
        local func_sig = parse_signature(signature)
        assert(func_sig.fname == clsname)
        local con_info = cs.get_constructor(type_info, func_sig.partypes)
        assert(con_info, func_sig.fname)
        def.add_class_method(CONSTRUCTOR, function(...)
            local this = cs.call_method(con_info, nil, ...)
            return cls_mgr.make_instance(clsname, this)
        end)
    end

    ----------------
    -- METHODS
    ----------------
    for _, signature in ipairs(self.__methods) do
        local func_sig = parse_signature(signature)
        local mtd_info = cs.get_method(type_info, func_sig.fname,
            func_sig.partypes)
        assert(mtd_info, func_sig.fname)
        def.add_inst_method(func_sig.fname, function(self, ...)
            local this = rawget(self, "__this")
            return wrap_retval( cs.call_method(mtd_info, this, ...),
                func_sig.ret_typename )
        end)
    end

    ----------------
    -- STATIC METHODS
    ----------------
    for _, signature in ipairs(self.__static_methods) do
        local func_sig = parse_signature(signature)
        local mtd_info = cs.get_static_method(type_info, func_sig.fname,
            func_sig.partypes)
        assert(mtd_info, func_sig.fname)
        def.add_class_method(func_sig.fname, function(...)
            -- print("call static method:", func_sig.fname)
            local ok, val = pcall( cs.call_method, mtd_info, nil, ...)
            if not ok then
                error("call static method("..func_sig.fname..") error: "..val)
            end
            return wrap_retval( val, func_sig.ret_typename )
        end)
    end

    ----------------
    -- FIELDS
    ----------------
    for _, signature in ipairs(self.__fields) do
        local sig = parse_signature(signature)
        local field_info = cs.get_field(type_info, sig.fname)
        assert(field_info, sig.fname)
        local field_type = get_type(sig.ret_typename)
        local function index(self, key)
            local this = rawget(self, "__this")
            return wrap_retval( cs.get_field_value(field_info, this,
                field_type), sig.ret_typename )
        end
        local function newindex(self, key, value)
            local this = rawget(self, "__this")
            local raw_value = unwrap_param(value, sig.ret_typename)
            cs.set_field_value(field_info, this, raw_value, field_type)
        end
        -- sig.ret_typename
        def.add_inst_field(sig.fname, index, newindex)
    end

    ----------------
    -- PROPERTIES
    ----------------
    for _, signature in ipairs(self.__properties) do
        local sig = parse_signature(signature)
        local prop_info = cs.get_prop(type_info, sig.fname)
        assert(prop_info, sig.fname)
        local prop_type = get_type(sig.ret_typename)
        local function index(self, key)
            local this = rawget(self, "__this")
            return wrap_retval( cs.get_prop_value(prop_info, this,
                prop_type), sig.ret_typename )
        end
        local function newindex(self, key, value)
            local this = rawget(self, "__this")
            local raw_value = unwrap_param(value, sig.ret_typename)
            -- print("property __newindex", this, key, raw_value)
            cs.set_prop_value(prop_info, this, raw_value, prop_type)
        end
        def.add_inst_field(sig.fname, index, newindex)
    end

    ----------------
    -- STATIC PROPERTIES
    ----------------
    for _, signature in ipairs(self.__static_properties) do
        local sig = parse_signature(signature)
        local prop_info = cs.get_static_prop(type_info, sig.fname)
        assert(prop_info, sig.fname)
        local prop_type = get_type(sig.ret_typename)
        local function index(self, key)
            return wrap_retval( cs.get_prop_value(prop_info, nil,
                prop_type), sig.ret_typename )
        end
        local function newindex(self, key, value)
            local raw_value = unwrap_param(value, sig.ret_typename)
            cs.set_prop_value(prop_info, nil, raw_value, prop_type)
        end
        def.add_class_field(sig.fname, index, newindex)
    end

    return cls_mgr.make_class(clsname)
end

local class_mt = {
    __index = {
        constructor     = constructor,
        method          = method,
        static_method   = static_method,
        field           = field,
        property        = property,
        static_property = static_property,
    },
}

----------------------------------------------------------------------

local function assembly(builder, assembly)
    table.insert(builder.assembly_list, assembly)
end

local function using(builder, namespace)
    table.insert(builder.using_list, namespace)
end

local function class(builder, clsname)
    assert( not builder.class_list[clsname] )

    local inst = {
        __class_name = clsname,
        __constructor = {},
        __methods = {},
        __static_methods = {},
        __fields = {},
        __properties = {},
        __static_properties = {},
    }
    local obj = setmetatable(inst, class_mt)
    builder.class_list[clsname] = obj
    return obj
end

----------------------------------------------------------------------

local function new_builder()
    return {
        assembly_list = {},
        using_list = {},
        class_list = {},
    }
end

local function resolve_builder(builder)
    local mod = {}

    cs.clear_assembly_list()
    for _, assembly in ipairs(builder.assembly_list) do
        cs.add_assembly(assembly)
    end

    cs.clear_using_list()
    for _, namespace in ipairs(builder.using_list) do
        cs.using(namespace)
    end

    local cls_mgr = new_class_mgr()
    for clsname, class_info in pairs(builder.class_list) do
        cls_mgr.declare_class(clsname)
    end

    for clsname, class_info in pairs(builder.class_list) do
        mod[clsname] = build_class(cls_mgr, class_info)
    end

    return mod
end

local function build(init_func)
    local builder = new_builder()
    local wrap = function(func)
        return function(...) return func(builder, ...) end
    end
    local env = {
        assembly = wrap(assembly),
        using = wrap(using),
        class = wrap(class),
    }
    init_func(env)
    return resolve_builder(builder)
end

return {
    build = build,
}

