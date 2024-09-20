Shader "Hidden/Occluder"
{
    Properties
    {
      _Color("Main Color", Color) = (0.0, 0.0, 0.0, 0.0)
    }

        SubShader
    {
      Tags { "RenderType" = "Opaque" }
      ZWrite Off Cull Off
      Fog {Mode Off}
      Color[_Color]

      Pass {}
    }
}