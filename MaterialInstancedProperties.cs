using System.Collections.Generic;
using UnityEngine;

namespace Jick
{
	[ExecuteInEditMode,RequireComponent(typeof(MeshRenderer)),AddComponentMenu("Jick/Material Properties"),DisallowMultipleComponent]
	public class MaterialInstancedProperties : MonoBehaviour
	{
		[SerializeField] public List<MaterialProperty> Properties = new List<MaterialProperty>();
		[HideInInspector] private MeshRenderer _Mesh;
		[HideInInspector] private MaterialPropertyBlock _MatProps;

		private void Awake()
		{
			SetMaterialPropertyBlock();
		}

		private void OnDestroy()
		{
			if ( _Mesh == null )
			{
				_Mesh = GetComponent<MeshRenderer>();
			}

			if ( _Mesh != null )
			{
				_Mesh.SetPropertyBlock( null );
			}
		}

		#if UNITY_EDITOR
		private void Update()
		{
			SetMaterialPropertyBlock();
		}
		#endif

		private void SetMaterialPropertyBlock()
		{
			if ( _Mesh == null || _MatProps == null )
			{
				_Mesh = GetComponent<MeshRenderer>();
				_MatProps = new MaterialPropertyBlock();
				_Mesh.GetPropertyBlock( _MatProps );
			}

			if ( _MatProps != null )
			{
				if ( Properties != null && Properties.Count > 0 )
				{
					_MatProps.Clear();

					foreach ( MaterialProperty property in Properties )
					{
						if ( property.Change )
						{
							switch ( property.Type )
							{
								case "Color":
									_MatProps.SetColor( property.Name, property.ColorValue );
									break;

								case "Texture":
									if ( property.TextureValue != null ) _MatProps.SetTexture( property.Name, property.TextureValue );
									break;

								case "Float":
									_MatProps.SetFloat( property.Name, property.FloatValue );
									break;

								case "Vector":
									_MatProps.SetVector( property.Name, property.VectorValue );
									break;
							}
						}
					}

					_Mesh.SetPropertyBlock( _MatProps );
				}
			}
		}
	}

	[System.Serializable]
	public class MaterialProperty
	{
		[SerializeField] public string Name = "";
		[SerializeField] public string Desc = "";
		[SerializeField] public string Type = "";
		[SerializeField] public bool Change = false;
		[SerializeField] public Color ColorValue = Color.white;
		[SerializeField] public Texture TextureValue = null;
		[SerializeField] public float FloatValue = 0f;
		[SerializeField] public Vector4 VectorValue = Vector4.zero;

		public MaterialProperty( string name, string desc, Color value )
		{
			Name = name;
			Desc = desc;
			Type = "Color";
			ColorValue = value;
		}

		public MaterialProperty( string name, string desc, Texture value )
		{
			Name = name;
			Desc = desc;
			Type = "Texture";
			TextureValue = value;
		}

		public MaterialProperty( string name, string desc, float value )
		{
			Name = name;
			Desc = desc;
			Type = "Float";
			FloatValue = value;
		}

		public MaterialProperty( string name, string desc, Vector4 value )
		{
			Name = name;
			Desc = desc;
			Type = "Vector";
			VectorValue = value;
		}
	}
}
