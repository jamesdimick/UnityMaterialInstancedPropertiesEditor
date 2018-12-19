using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using SPT = UnityEditor.ShaderUtil.ShaderPropertyType;

namespace Jick
{
	[CustomEditor(typeof(MaterialInstancedProperties))]
	public class MaterialInstancedPropertiesEditor : Editor
	{
		private MaterialInstancedProperties _CastedTarget;
		private SerializedProperty _Properties;
		private Material _Material;
		private Shader _Shader;

		private void OnEnable()
		{
			_CastedTarget = (MaterialInstancedProperties)target;
			_Properties = serializedObject.FindProperty( "Properties" );

			if ( _CastedTarget.GetComponent<MeshRenderer>() != null && _CastedTarget.GetComponent<MeshRenderer>().sharedMaterial != null )
			{
				_Material = _CastedTarget.GetComponent<MeshRenderer>().sharedMaterial;
				_Shader = _Material.shader;
			}

			PopulatePropertiesFromShader();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			if ( _Properties == null )
			{
				_Properties = serializedObject.FindProperty( "Properties" );
			}

			if ( _Properties != null && _Shader != null )
			{
				if ( _Properties.arraySize > 0 )
				{
					List<SerializedProperty> finalProps = new List<SerializedProperty>();
					for ( int i = 0; i < _Properties.arraySize; i++ )
					{
						SerializedProperty prop = _Properties.GetArrayElementAtIndex( i );

						if ( prop != null && prop.FindPropertyRelative( "Name" ) != null )
						{
							if ( ShaderUtilExtensions.HasShaderProperty( _Shader, prop.FindPropertyRelative( "Name" ).stringValue ) )
							{
								finalProps.Add( prop );
							}
						}
					}
					finalProps = finalProps.OrderBy( x => ShaderUtilExtensions.GetShaderPropertyNames( _Shader ).IndexOf( x.FindPropertyRelative( "Name" ).stringValue ) ).ToList();

					EditorGUILayout.Space();

					for ( int j = 0; j < finalProps.Count; j++ )
					{
						SerializedProperty prop = finalProps[j];
						string name = prop.FindPropertyRelative( "Name" ).stringValue;
						string desc = prop.FindPropertyRelative( "Desc" ).stringValue;
						string type = prop.FindPropertyRelative( "Type" ).stringValue;
						bool change = prop.FindPropertyRelative( "Change" ).boolValue;
						string[] attrs = ShaderUtilExtensions.GetShaderPropertyAttributes( _Shader, name ) ?? new string[0];
						SerializedProperty p = prop.FindPropertyRelative( $"{type}Value" );
						GUIContent label = new GUIContent( desc, name );

						EditorGUILayout.BeginHorizontal();

						EditorGUI.BeginChangeCheck();
						var overrideRect = GUILayoutUtility.GetRect( 17f, 17f, GUILayout.ExpandWidth( false ) );
						overrideRect.yMin += 4f;
						bool chnge = GUI.Toggle( overrideRect, change, CoreEditorUtils.GetContent( "|Override this property for this object." ), CoreEditorStyles.smallTickbox );
						if ( EditorGUI.EndChangeCheck() )
						{
							prop.FindPropertyRelative( "Change" ).boolValue = chnge;
						}

						EditorGUI.BeginDisabledGroup( !change );

						switch ( type )
						{
							case "Color":
								EditorGUI.BeginChangeCheck();
								Color valColor = EditorGUILayout.ColorField( label, p.colorValue, true, true, true );
								if ( EditorGUI.EndChangeCheck() )
								{
									p.colorValue = valColor;
								}
								break;

							case "Texture":
								EditorGUI.BeginChangeCheck();
								Texture valTexture = (Texture) EditorGUILayout.ObjectField( label, p.objectReferenceValue, typeof( Texture ), false );
								if ( EditorGUI.EndChangeCheck() )
								{
									p.objectReferenceValue = valTexture;
								}
								break;

							case "Float":
								EditorGUI.BeginChangeCheck();
								float valFloat = attrs.Contains( "Toggle", StringComparer.OrdinalIgnoreCase ) ? Convert.ToSingle( EditorGUILayout.Toggle( label,
								                 Convert.ToBoolean( Mathf.Clamp01( p.floatValue ) ) ) ) : EditorGUILayout.FloatField( label, p.floatValue );
								if ( EditorGUI.EndChangeCheck() )
								{
									p.floatValue = valFloat;
								}
								break;

							case "Vector":
								EditorGUI.BeginChangeCheck();
								Vector4 valVector = EditorGUILayout.Vector4Field( label, p.vector4Value );
								if ( EditorGUI.EndChangeCheck() )
								{
									p.vector4Value = valVector;
								}
								break;
						}

						EditorGUI.EndDisabledGroup();

						EditorGUILayout.EndHorizontal();
					}
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void PopulatePropertiesFromShader()
		{
			if ( _Shader != null )
			{
				int propertyCount = ShaderUtil.GetPropertyCount( _Shader );

				if ( propertyCount > 0 )
				{
					for ( int i = 0; i < propertyCount; i++ )
					{
						string name = ShaderUtil.GetPropertyName( _Shader, i );

						if ( !_CastedTarget.Properties.Any( x => x.Name == name ) )
						{
							string desc = ShaderUtil.GetPropertyDescription( _Shader, i );

							MaterialProperty newProp;
							switch ( ShaderUtil.GetPropertyType( _Shader, i ) )
							{
								case SPT.Color:
									newProp = new MaterialProperty( name, desc, _Material.GetColor( name ) );
									break;

								case SPT.TexEnv:
									newProp = new MaterialProperty( name, desc, _Material.GetTexture( name ) );
									break;

								case SPT.Vector:
									newProp = new MaterialProperty( name, desc, _Material.GetVector( name ) );
									break;

								case SPT.Float:
								case SPT.Range:
								default:
									newProp = new MaterialProperty( name, desc, _Material.GetFloat( name ) );
									break;
							}

							_CastedTarget.Properties.Add( newProp );
						}
					}
				}
			}
		}
	}

	public static class ShaderUtilExtensions
	{
		private delegate string[] GetShaderPropertyAttributesDelegate( Shader s, string name );
		private static GetShaderPropertyAttributesDelegate _getShaderPropertyAttributes;

		/// <summary>
		/// Returns an array of strings representing every attribute on the given property of the given <see cref="UnityEngine.Shader"/>.
		/// </summary>
		public static string[] GetShaderPropertyAttributes( Shader s, string name )
		{
			if ( _getShaderPropertyAttributes == null )
			{
				Type type = typeof( ShaderUtil );
				MethodInfo methodInfo = type.GetMethod( "GetShaderPropertyAttributes", BindingFlags.Static | BindingFlags.NonPublic );

				_getShaderPropertyAttributes = (GetShaderPropertyAttributesDelegate)Delegate.CreateDelegate( typeof( GetShaderPropertyAttributesDelegate ), methodInfo );
			}

			return _getShaderPropertyAttributes( s, name );
		}

		/// <summary>
		/// Returns a <see cref="System.Collections.Generic.List{string}"/> of all properties in the given <see cref="UnityEngine.Shader"/>.
		/// </summary>
		public static List<string> GetShaderPropertyNames( Shader s )
		{
			List<string> output = new List<string>();
			int count = ShaderUtil.GetPropertyCount( s );

			if ( count > 0 )
			{
				for ( int i = 0; i < count; i++ )
				{
					output.Add( ShaderUtil.GetPropertyName( s, i ) );
				}
			}

			return output;
		}

		/// <summary>
		/// Returns whether the given <see cref="UnityEngine.Shader"/> contains a property matching the given name.
		/// </summary>
		public static bool HasShaderProperty( Shader s, string name )
		{
			for ( int i = 0; i < ShaderUtil.GetPropertyCount( s ); i++ )
			{
				if ( ShaderUtil.GetPropertyName( s, i ) == name )
				{
					return true;
				}
			}

			return false;
		}
	}
}
