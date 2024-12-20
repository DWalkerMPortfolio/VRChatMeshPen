Shader "Custom/Eraseable Mesh Pen Line"
{
    Properties
    {
        _EraseRadius ("Erase Radius", Float) = 1
        _DebugColor ("Debug Color", Color) = (1,1,1,1)
        _MarkRadius ("Mark Radius", Float) = 1
        _MarkOutlineThickness ("Mark Outline Thickness", Float) = 0.1
        _MarkOutlineColor ("Mark Outline Color", Color) = (1,1,1,1)
        _MarkInteriorColor ("Mark Interior Color", Color) = (0.5, 0.5, 0.5, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            float3 _Points[25];
            float3 _EraserPosition;
            float _EraseRadius;
            fixed4 _DebugColor;
            float _MarkRadius;
            float _MarkOutlineThickness;
            fixed4 _MarkOutlineColor;
            fixed4 _MarkInteriorColor;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR0;
                float4 worldPos : TEXCOORD2;
                float4 clip : TEXCOORD1;
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.color = v.color;
                //o.color = _DebugColor;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.clip = v.vertex.y < -10 ? 1:0;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float inRadius = 1;

                //Clip if within range
                for (int x=0; x<25; x++)
                {
                    float3 distanceVector = i.worldPos - _Points[x];
                    float squaredDistanceFromErase = dot(distanceVector, distanceVector);
                    inRadius *= step(_EraseRadius * _EraseRadius, squaredDistanceFromErase); //inRadius gets set to 0 if it is ever inside the radius
                }

                //Compare against actual eraser position
                float3 distanceVector = i.worldPos - _EraserPosition;
                float squaredDistanceFromErase = dot(distanceVector, distanceVector);
                inRadius *= step(_EraseRadius * _EraseRadius, squaredDistanceFromErase);
                
                clip(inRadius < 0.1f || i.clip > 0 ? -1:1); //Clip the fragment if inside any radius

                //Set color, highlighting the area inside the marking radius
                fixed4 col = lerp(_MarkInteriorColor, i.color, step(_MarkRadius * _MarkRadius, squaredDistanceFromErase));
                //Highlight outline around marking radius
                col = lerp(_MarkOutlineColor, col, smoothstep(_MarkOutlineThickness * _MarkOutlineThickness, _MarkOutlineThickness * _MarkOutlineThickness + _MarkOutlineThickness/10.0, abs(squaredDistanceFromErase - _MarkRadius * _MarkRadius)));

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
