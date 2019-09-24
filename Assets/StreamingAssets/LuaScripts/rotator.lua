local unitylib = require "unitylib.cs"

local function rotate(self, vector)
    unitylib.Rotate(self._game_object, vector);
end

return {
    rotate = rotate
}