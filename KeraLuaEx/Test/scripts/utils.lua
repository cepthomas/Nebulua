


local _cache = {}
local function is_array(t)
  local count = #t
  if count == 0 then
    return false
  end

  for i=1,count do
    local v = t[i]
    if v == nil then
      return false
    end
    t[i] = nil
    _cache[i] = v
  end

  if next(t) then
    return false
  end

  for i=1,count do
    t[i] = _cache[i]
    _cache[i] = nil
  end

  return true
end
