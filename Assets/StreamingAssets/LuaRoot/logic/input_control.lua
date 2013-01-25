local SceneMgr      = require "logic.scene_mgr"

local UnityEngine   = require "lib.unity_engine"
local Input         = UnityEngine.Input

local x = 0
local y = 0

local function update_input()
    -- print( "vertical: ",   Input.GetAxis("Vertical") )
    -- print( "horizontal: ", Input.GetAxis("Horizontal") )
    local scene = SceneMgr.get_scene()
    x = x + Input.GetAxis("Horizontal") * 50
    y = y + Input.GetAxis("Vertical") * 50
    scene.hero:move( x, y )
end

return {
    update_input = update_input,
}

