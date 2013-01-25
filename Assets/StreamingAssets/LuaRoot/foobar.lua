
local function foobar(...)
	print("foobar", ...)
end

return {
	foobar = foobar,
}
