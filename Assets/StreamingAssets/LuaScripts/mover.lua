local unitylib = require "unitylib.cs"

local function translate(self, vector)
    unitylib.Translate(self._game_object, vector);
    --unitylib.Debug(vector.x);
end

return {
    translate = translate
}