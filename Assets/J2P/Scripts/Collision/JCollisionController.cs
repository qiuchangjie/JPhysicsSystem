﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace J2P
{
	public delegate void CollisionEvent( CollisionInfo collisionInfo );

	public class JCollisionController : MonoBehaviour
	{
		[HideInInspector]
		public int collisionMask;

		public int horizontalRayCount = 4;

		public int verticalRayCount = 4;

		//进行射线检测时,把起点放在原碰撞框的顶点再往里缩这个距离
		public float _shrinkWidth = 0.1f;

		public CollisionEvent onCollisionEnter;

		public CollisionEvent onCollisionExit;

		public CollisionEvent onTriggerEnter;

		public CollisionEvent onTriggerExit;

		public new Collider2D collider { get; private set; }

		protected Bounds _bounds { get { return collider.bounds; } }

		public bool showDebugGizoms = false;

		protected float _horizontalRaySpace;

		protected float _verticalRaySpace;

		protected RaycastOrigins _raycastOrigins = new RaycastOrigins();

		protected RaycastHit2D[] _raycastHit2D;

		protected virtual int _maxHitCollidersCount
		{
			get
			{
				return 20;
			}
		}

		/// <summary>
		/// Colliders that won't collide with this collider
		/// </summary>
		protected HashSet<Collider2D> _ignoredColliders = new HashSet<Collider2D>();

		protected Transform _transform;

		protected virtual void Awake()
		{
			this.collider = this.gameObject.GetComponent<Collider2D>();
			_raycastHit2D = new RaycastHit2D[_maxHitCollidersCount];
			_transform = this.gameObject.transform;
		}

		private void OnDestroy()
		{
			onCollisionEnter.ClearAllDelegates();
			onCollisionExit.ClearAllDelegates();
			onTriggerEnter.ClearAllDelegates();
			onCollisionExit.ClearAllDelegates();
		}

		public virtual void Simulate( float deltaTime )
		{
		}

		protected void CalculateRaySpace( ref Bounds bounds )
		{
			bounds.Expand( _shrinkWidth * -2 );
			var boundsSize = bounds.size;
			_horizontalRaySpace = boundsSize.y / ( this.horizontalRayCount - 1 );
			_verticalRaySpace = boundsSize.x / ( this.verticalRayCount - 1 );
		}

		protected void UpdateRaycastOrigins()
		{
			//把原碰撞框内缩返回一个新的碰撞框,记录新碰撞框四个顶点用于射线检测的起点
			var bounds = _bounds;
			this.CalculateRaySpace( ref bounds );

			// Top Left
			_raycastOrigins.topLeft.x = bounds.min.x;
			_raycastOrigins.topLeft.y = bounds.max.y;

			// Top Right
			_raycastOrigins.topRight.x = bounds.max.x;
			_raycastOrigins.topRight.y = bounds.max.y;

			// Bottom Left
			_raycastOrigins.bottomLeft.x = bounds.min.x;
			_raycastOrigins.bottomLeft.y = bounds.min.y;

			// Bottom Right
			_raycastOrigins.bottomRight.x = bounds.max.x;
			_raycastOrigins.bottomRight.y = bounds.min.y;
		}
	}
}
