local function vector_mul(vector, num)
    return{
    x = vector.x * num;
    y = vector.y * num;
    z = vector.z * num;
    }
end

return {
    vector_mul = vector_mul
}