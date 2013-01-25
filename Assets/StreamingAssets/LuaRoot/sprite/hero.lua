local UnityEngine       = require "lib.unity_engine"
local GameObject        = UnityEngine.GameObject
local MeshFilter        = UnityEngine.MeshFilter
local Resources         = UnityEngine.Resources
local Mesh              = UnityEngine.Mesh
local Vector3           = UnityEngine.Vector3
local MeshRenderer      = UnityEngine.MeshRenderer
local Material          = UnityEngine.Material

local function create()
    local component

    local mesh = Resources.Load("Mesh/Quad1x1W1L1VC", Mesh._Type())
    local material = Resources.Load("Material/Sprite", Material._Type())

    local unity_obj = GameObject._New("HERO")
    component = unity_obj:AddComponent(MeshFilter._Type())
    local mesh_filter = MeshFilter._ConvertFrom(component)
    print("mesh_filter:", mesh_filter:ToString())
    print("mesh_filter.mesh:", mesh_filter.mesh)
    mesh_filter.sharedMesh = Mesh._ConvertFrom(mesh)
    unity_obj.transform.localScale = Vector3._New(128,128,128)

    component = unity_obj:AddComponent(MeshRenderer._Type())
    local mesh_renderer = MeshRenderer._ConvertFrom(component)
    mesh_renderer.castShadows = false
    mesh_renderer.receiveShadows = false
    mesh_renderer.material = Material._ConvertFrom(material)

    local mt = {
        __index = {
            move = function(self, x, y)
                local unity_obj = rawget(self, "__unity_obj")
                unity_obj.transform.localPosition = Vector3._New(x, y, 0)
            end
        },
        __newindex = function(self, key, value)
        end,
    }
    return setmetatable({
        __unity_obj = unity_obj,
    }, mt)
end

return {
    create = create,
}

