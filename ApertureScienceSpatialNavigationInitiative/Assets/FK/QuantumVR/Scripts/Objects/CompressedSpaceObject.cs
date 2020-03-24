using System;
using FK.Utility.Binary;
using UnityEngine;
using System.Collections.Generic;
using FK.Utility.ArraysAndLists;

namespace FK.QuantumVR.Objects
{
	/// <summary>
	/// <para>A rendered object that uses Compressed space</para>
	///
	/// v1.2 11/2019
	/// Written by Fabian Kober
	/// fabian-kober@gmx.net
	/// </summary>
	public class CompressedSpaceObject : CompressedSpaceEntity
	{
		// ######################## PROPERTIES ######################## //
		public bool HasRenderer => _hasRenderer;


		// ######################## PRIVATE VARS ######################## //
		private Renderer _renderer;
		private List<Material> _originalMaterials;
		private bool _hasRenderer;

		private Guid _traversingPortalTargetCell;

		/// <summary>
		/// The object used for collision on the other portal side when going through a portal
		/// </summary>
		private GameObject _portalCollisionObject;


		// ######################## UNITY EVENT FUNCTIONS ######################## //
		protected override void Awake()
		{
			base.Awake();
			Init();
		}


		// ######################## INITS ######################## //
		///<summary>
		/// Does the Init for this Behaviour
		///</summary>
		private void Init()
		{
			_renderer = GetComponent<Renderer>();
			_hasRenderer = _renderer;

			if (!_hasRenderer)
				return;

			_originalMaterials = new List<Material>(_renderer.sharedMaterials);
		}


		// ######################## FUNCTIONALITY ######################## //
		protected override void OnLayerMaskChanged(byte spatialLayermask, bool isInActiveCell)
		{
			if (!_hasRenderer)
				return;

			bool belongsToActiveCell = isInActiveCell;
			if (IsInsidePortal)
			{
				spatialLayermask |= SpatialCellManagerInstance.GetCellSpatialLayermask(_traversingPortalTargetCell);
				isInActiveCell |= SpatialCellManagerInstance.IsActiveCell(_traversingPortalTargetCell);

				// if we were previously going through another portal we need to dispose of these materials first
				if (_hasRenderer)
					MaterialManager.DisposePortalTraversalMaterials(this, _originalMaterials);

			}

			MaterialManager.SetCompressedSpaceObjectMaterials(_renderer, this, spatialLayermask, isInActiveCell, _originalMaterials, IsInsidePortal, belongsToActiveCell, TraversingPortal);
		}

		public override void EnterPortal(Portal portal, Guid targetCell, bool doUpdate = true)
		{
			if (TraversingPortal)
				return;

			// if we were previously going through another portal we need to dispose of these materials first
			if (_hasRenderer && TraversingPortal != null && TraversingPortal != portal)
				MaterialManager.DisposePortalTraversalMaterials(this, _originalMaterials);

			base.EnterPortal(portal, targetCell, doUpdate);

			_traversingPortalTargetCell = targetCell;

			// if we do not yet have a collision object, create one
			if (_portalCollisionObject == null)
			{
				// create and parent collision object
				_portalCollisionObject = new GameObject($"{name}_portalTraversalCollision");
				_portalCollisionObject.transform.parent = transform;
				_portalCollisionObject.transform.localPosition = Vector3.zero;
				_portalCollisionObject.transform.localRotation = Quaternion.identity;
				_portalCollisionObject.transform.localScale = Vector3.one;

				// get all colliders and copy them
				Collider[] colliders = GetComponents<Collider>();
				foreach (Collider col in colliders)
				{
					// don't copy triggers
					if (col.isTrigger)
						continue;

					Collider copy = null;
					switch (col)
					{
						case SphereCollider sphereCollider:
							{
								SphereCollider sphereCopy = _portalCollisionObject.AddComponent<SphereCollider>();
								copy = sphereCopy;
								sphereCopy.center = sphereCollider.center;
								sphereCopy.radius = sphereCollider.radius;
								break;
							}
						case BoxCollider boxCollider:
							{
								BoxCollider boxCopy = _portalCollisionObject.AddComponent<BoxCollider>();
								copy = boxCopy;
								boxCopy.center = boxCollider.center;
								boxCopy.size = boxCollider.size;
								break;
							}
						case CapsuleCollider capsuleCollider:
							{
								CapsuleCollider capsuleCopy = _portalCollisionObject.AddComponent<CapsuleCollider>();
								copy = capsuleCopy;
								capsuleCopy.center = capsuleCollider.center;
								capsuleCopy.direction = capsuleCollider.direction;
								capsuleCopy.height = capsuleCollider.height;
								capsuleCopy.radius = capsuleCollider.radius;
								break;
							}
						case MeshCollider meshCollider:
							{
								MeshCollider meshCopy = _portalCollisionObject.AddComponent<MeshCollider>();
								copy = meshCopy;
								meshCopy.sharedMesh = meshCollider.sharedMesh;
								meshCopy.convex = meshCollider.convex;
								meshCopy.cookingOptions = meshCollider.cookingOptions;
								break;
							}
					}

					if (copy != null)
						copy.sharedMaterial = col.sharedMaterial;
				}
			}

			// set layer of collision object to the layer of the cell the portal leads to and activate it
			if (SpatialCellManagerInstance.IsActiveCell(targetCell))
			{
				_portalCollisionObject.layer = OriginalUnityLayer;
				_portalCollisionObject.SetActive(true);
			}
			else
			{
				int layerIndex = SpatialCellManagerInstance.GetCellSpatialLayermask(_traversingPortalTargetCell)
					.GetLeastSignificantBitPosition();

				if (layerIndex > -1)
				{
					_portalCollisionObject.layer = SpatialCellManagerInstance.UnityLayersBySpatialLayer[layerIndex];
					_portalCollisionObject.SetActive(true);
				}
				else
				{
					_portalCollisionObject.SetActive(false);
				}
			}
			

			// update children
			foreach (Transform child in transform)
			{
				child.GetComponent<CompressedSpaceEntity>()?.EnterPortal(portal, targetCell, doUpdate);
			}

			if (doUpdate)
				UpdateMaterials();
		}

		public override void ExitPortal(Portal portal)
		{
			// we need to store the portal because it will be cleared in the base function
			Portal traversingPortal = TraversingPortal;
			base.ExitPortal(portal);

			// if the exited portal is not the one we are traversing we can ignore this call
			if (traversingPortal != portal)
				return;

			// disable collision and update children
			_portalCollisionObject.SetActive(false);
			foreach (Transform child in transform)
			{
				child.GetComponent<CompressedSpaceEntity>()?.ExitPortal(portal);
			}

			if (!_hasRenderer)
				return;

			// update rendering and clean up
			UpdateMaterials();
			MaterialManager.DisposePortalTraversalMaterials(this, _originalMaterials);
		}

		public void AddMaterial(Material material)
		{
			if (!_hasRenderer)
				return;

			if (IsInsidePortal)
				MaterialManager.DisposePortalTraversalMaterials(this, _originalMaterials);

			_originalMaterials.Add(material);
			UpdateMaterials();
		}

		public void RemoveMaterial(Material material)
		{
			if (!_hasRenderer || !_originalMaterials.Contains(material))
				return;

			if (IsInsidePortal)
				MaterialManager.DisposePortalTraversalMaterials(this, _originalMaterials);

			_originalMaterials.Remove(material);
			UpdateMaterials();
		}


		// ######################## GETTER ######################## //
		public Material[] GetOriginalMaterials()
		{
			return !_hasRenderer ? null : _originalMaterials.ToArray();
		}

		// ######################## UTILITY ######################## //
		private void UpdateMaterials()
		{
			OnLayerMaskChanged(SpatialCellManagerInstance.GetCellSpatialLayermask(_spatialCellGuid.Value), SpatialCellManagerInstance.IsActiveCell(_spatialCellGuid.Value));
		}


		public Material[] GetCurrentInstancesOfMaterial(Material originalMaterial)
		{
			Material[] allInstances = MaterialManager.GetMaterialInstances(originalMaterial);
			List<Material> instancesOnThisObject = new List<Material>();
			foreach (Material instance in allInstances)
			{
				if (_renderer.sharedMaterials.Search(instance) > -1)
					instancesOnThisObject.Add(instance);
			}

			return instancesOnThisObject.ToArray();
		}

	}
}