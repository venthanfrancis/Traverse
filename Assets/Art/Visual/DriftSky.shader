Shader "DRIFT/Quiet Sky"
{
 Properties { _Tint ("Sky tint",Color)=(.02,.03,.08,1) _Horizon("Horizon",Color)=(.08,.1,.2,1) _Stars("Stars",Range(0,1))=1 _Eclipse("Eclipse",Range(0,1))=0 }
 SubShader {
 Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
 Cull Off ZWrite Off
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION; }; struct v2f { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; };
 float4 _Tint,_Horizon; float _Stars,_Eclipse;
 v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.direction=v.vertex.xyz; return o; }
 float hash(float3 p) { return frac(sin(dot(p,float3(12.9898,78.233,45.164)))*43758.5453); }
 half4 frag(v2f i):SV_Target {
 float3 d=normalize(i.direction); float horizon=pow(1-abs(d.y),3);
 float3 color=lerp(_Tint.rgb,_Horizon.rgb,horizon);
 float3 cell=floor(d*240); float3 f=frac(d*240)-.5;
 float star=step(.996,hash(cell))*pow(saturate(1-length(f)*2.8),6)*_Stars;
 color+=star*1.6;
 float cloud=pow(saturate(.5+.5*sin(d.x*7+d.z*4)*sin(d.y*5-d.z*3)),3);
 color+=float3(.035,.012,.05)*cloud*_Stars;
 // Static angular eclipse: no meshes, particle simulation or full-screen pass.
 float angular=acos(clamp(dot(d,normalize(float3(0,.10,1))),-1,1));
 float edge=abs(angular-.135);
 float halo=exp(-edge*55)*.35+exp(-edge*450)*3;
 float disk=1-smoothstep(.132,.136,angular);
 color=lerp(color,float3(.009,.005,.006),disk*_Eclipse);
 color+=float3(1,.055,.008)*halo*_Eclipse*(1-disk);
 return half4(color,1); }
 ENDHLSL
 } }
}

