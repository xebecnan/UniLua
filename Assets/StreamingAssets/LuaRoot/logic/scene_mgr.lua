local Hero = require "sprite.hero"

local Scene

local function create_hero()
    return Hero.create()
end

local function init_scene()
    Scene = {}
    Scene.hero = create_hero()
    return Scene
end

local function get_scene()
    return Scene
end

return {
    init_scene = init_scene,
    get_scene  = get_scene,
}

