struct VertexToPixel
{
	float4 Position     : POSITION;    
	float2 TexCoords    : TEXCOORD0;
	float3 Normal        : TEXCOORD1;
	float3 Position3D    : TEXCOORD2;
};

struct PixelToFrame
{
	float4 Color : COLOR0;
};

float4x4 xWorldViewProjection;

float4x4 xRot;
float4 xLightPos;
float xLightPower;


Texture xColoredTexture;

sampler ColoredTextureSampler = sampler_state { texture = <xColoredTexture> ; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = mirror; AddressV = mirror;};

VertexToPixel SimplestVertexShader( float4 inPos : POSITION, float2 inTexCoords : TEXCOORD0, float3 inNormal : NORMAL)
{
	VertexToPixel Output = (VertexToPixel)0;
    
	Output.Position = mul(inPos, xWorldViewProjection);
	Output.TexCoords = inTexCoords;

	Output.Normal = mul(inNormal, xRot);
	Output.Position3D = inPos;

	return Output;    
}


float DotProduct(float4 LightPos, float3 Pos3D, float3 Normal)
{
	float3 LightDir = normalize(LightPos - Pos3D);
	return dot(LightDir, Normal);
}


PixelToFrame OurFirstPixelShader(VertexToPixel PSIn)
{
	PixelToFrame Output = (PixelToFrame)0;
	float DiffuseLightingFactor = DotProduct(xLightPos, PSIn.Position3D, PSIn.Normal);
	Output.Color = tex2D(ColoredTextureSampler, PSIn.TexCoords)*DiffuseLightingFactor*xLightPower;
	return Output;
}

technique Simplest
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 SimplestVertexShader();
		PixelShader = compile ps_2_0 OurFirstPixelShader();
	}
}