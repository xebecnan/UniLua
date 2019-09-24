local unitylib = require "unitylib.cs"
local unityhelper = require "unity_helper"

local rotate = function(self, euler_vec)
    self._game_object1.transform.eulerAngles = unitylib.Rotate(self._game_object1, euler_vec.x, euler_vec.y, euler_vec.z);

    local euler_angles = self._game_object1.transform.eulerAngles;
    self._game_object2.transform.eulerAngles = unityhelper.vector_mul(euler_angles, 0.5);
end

local scale = function(self, vector1, vector2)
    self._game_object1.transform.localScale = vector1;
    self._game_object2.transform.localScale = vector2;
end

local make = function(game_object1, game_object2)
  return 
  {
    _game_object1 = game_object1;
    _game_object2 = game_object2;
    rotate = rotate;
    scale = scale;
  }
end

return
{
  make = make;
}