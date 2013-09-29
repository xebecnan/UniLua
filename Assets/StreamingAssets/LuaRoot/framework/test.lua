local function awake()
    print("---- awake ----")
end

local function start()
    print("---- start ----")
    _U = true
    dofile("test/all.lua")
end

local function update()
end

local function late_update()
end

local function fixed_update()
end

return {
    awake           = awake,
    start           = start,
    update          = update,
    late_update     = late_update,
    fixed_update    = fixed_update,
}
