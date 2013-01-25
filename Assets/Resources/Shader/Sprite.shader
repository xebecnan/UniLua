// Custom sprite shader - no lighting, on/off alpha

Shader "Sprite" {
Properties {
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
//    LOD 100

    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha 
    Lighting Off

    Pass {
        SetTexture [_MainTex] { combine texture } 
    }
}
}

