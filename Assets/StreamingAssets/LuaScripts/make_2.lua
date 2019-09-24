local mover = require("mover")
local rotator = require("rotator")

local transform = function(self, move_vec, rotate_vec)
  mover.translate(self, move_vec);
  rotator.rotate(self, rotate_vec);
end -- rotate

local make = function(game_object, speed)
  return 
  {
    _game_object = game_object;
    transform = transform;
  }
end

--

return
{
  make = make;
}
