local ffi = require("lib.ffi")

local function _init(_ENV)
    assembly("mscorlib")
    assembly("UnityEngine")

    using("System")
    using("UnityEngine")

    class("Object")

    class("Vector3")
        :constructor("Vector3(float, float, float)")
        :field("float x")
        :field("float y")
        :field("float z")

    class("GameObject")
        :constructor("GameObject(String)")
        :method("Component AddComponent(Type)")
        :property("Transform transform")

    class("Transform")
        :property("Vector3 localScale")
        :property("Vector3 localPosition")

    class("Input")
        :static_method("float GetAxis(String)")

    class("Material")

    class("Shader")

    class("MeshFilter")
        :method("String ToString()")
        :property("Mesh mesh")
        :property("Mesh sharedMesh")

    class("MeshRenderer")
        :property("bool castShadows")
        :property("bool receiveShadows")
        :property("Material material")

    class("Resources")
        :static_method("Object Load(String,Type)")

    class("Mesh")

    class("Component")
end

return ffi.build(_init)

--[[
ffi.using("System")
ffi.using("UnityEngine")

local GameObject = ffi.class("UnityEngine.GameObject")
    :constructor("string")
    :method("AddComponent")
    :build()

local Input = ffi.class("UnityEngine.Input")
	:static_method("GetAxis")
	:build()


return {
    GameObject = GameObject,
    Input = Input,
}
]]

