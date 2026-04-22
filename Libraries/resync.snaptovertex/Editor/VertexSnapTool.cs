using Editor;
using Editor.MapEditor;
using Sandbox;
using Sandbox.Editor;
using System.Collections.Generic;
using System.Linq;

namespace Sandbox.Editor
{
	[EditorTool( "vertex_snap_tool" )]
	[Title( "Vertex Snap" )]
	[Icon( "open_with" )]
	public sealed class VertexSnapTool : EditorTool
	{
		public override void OnUpdate()
		{
			var selected = Selection.OfType<GameObject>().FirstOrDefault();
			if ( !selected.IsValid() ) return;

			using ( Gizmo.Scope( "move_manip", selected.WorldTransform ) )
			{
				if ( Gizmo.Control.Position( "pos", Vector3.Zero, out var nextPos ) )
				{
					using var undoScope = SceneEditorSession.Active.UndoScope( "Move Object" )
						.WithGameObjectChanges( selected, GameObjectUndoFlags.Properties )
						.Push();

					selected.WorldPosition = selected.WorldTransform.PointToWorld( nextPos );
				}
			}

			if ( !Gizmo.IsCtrlPressed ) return; //waiting for facepunch to add support for detecting inputs or keycodes in editor. (if they add this just change it to "Voice" so you can snap with "V" for example.

			var sourceShape = selected.Components.GetInChildrenOrSelf<Collider>()?.KeyframeBody?.Shapes?.FirstOrDefault(); //dunno how to get shapes from colliders.
			if ( sourceShape == null ) return;

			sourceShape.Triangulate( out var sourceVerts, out _ );
			if ( sourceVerts == null ) return;

			var sourceTxNoScale = new Transform( selected.WorldPosition, selected.WorldRotation, 1.0f );

			using ( Gizmo.Scope( "source_verts" ) )
			{
				Gizmo.Draw.Color = Color.Yellow.WithAlpha( 0.6f );
				foreach ( var v in sourceVerts )
				{
					Gizmo.Draw.SolidSphere( sourceTxNoScale.PointToWorld( v ), 2.5f );
				}
			}

			var ray = Gizmo.CurrentRay;
			var tr = Scene.Trace.Ray( ray, 10000f ).IgnoreGameObject( selected ).Run();

			if ( !tr.Hit || tr.Shape == null ) return;

			tr.Shape.Triangulate( out var targetVerts, out _ );
			if ( targetVerts != null )
			{
				var targetTxNoScale = new Transform( tr.GameObject.WorldPosition, tr.GameObject.WorldRotation, 1.0f );

				using ( Gizmo.Scope( "target_verts" ) )
				{
					Gizmo.Draw.Color = Color.Cyan.WithAlpha( 0.6f );
					foreach ( var v in targetVerts )
					{
						Gizmo.Draw.SolidSphere( targetTxNoScale.PointToWorld( v ), 2.5f );
					}
				}

				Vector3 sourceVtxWorld = GetClosestVertex( sourceVerts, sourceTxNoScale, tr.EndPosition );
				Vector3 targetVtxWorld = GetClosestVertex( targetVerts, targetTxNoScale, tr.EndPosition );

				Vector3 offset = targetVtxWorld - sourceVtxWorld;
				if ( offset.Length > 0.1f )
				{
					using var undoScope = SceneEditorSession.Active.UndoScope( "Vertex Snap" ) //sometimes you have to undo like 30 times to put it in default state. sometimes it's just works fine! whaaaaaat.
						.WithGameObjectChanges( selected, GameObjectUndoFlags.Properties )
						.Push();

					selected.WorldPosition += offset;
				}

				using ( Gizmo.Scope( "snap_highlight" ) )
				{
					Gizmo.Draw.Color = Color.White;
					Gizmo.Draw.Line( sourceVtxWorld, targetVtxWorld );
					Gizmo.Draw.Color = Color.Cyan; //vertexes of goal point
					Gizmo.Draw.SolidSphere( targetVtxWorld, 3.5f );
					Gizmo.Draw.Color = Color.Yellow; //vertexes of selected point
					Gizmo.Draw.SolidSphere( sourceVtxWorld, 3.5f );
				}
			}
		}

		private Vector3 GetClosestVertex( Vector3[] positions, Transform txNoScale, Vector3 referencePoint )
		{
			Vector3 closest = Vector3.Zero;
			float minDistance = float.MaxValue;
			foreach ( var localPos in positions )
			{
				var worldPos = txNoScale.PointToWorld( localPos );
				float dist = worldPos.Distance( referencePoint );
				if ( dist < minDistance ) { minDistance = dist; closest = worldPos; }
			}
			return closest;
		}
	}

}
