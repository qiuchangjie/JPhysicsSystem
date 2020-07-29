﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace J2P
{
	public class JRigidbody : JCollisionController
	{
		public float gravityScale = 1.0f;

		private Vector2 _velocity;

		private CollisionInfo _collisionInfo;

		private CollisionInfo _triggerInfo;

		private bool _colliderIsTrigger = false;

		private JPhysicsManager _physicsManager;

		private Vector3 _movement = Vector3.zero;

		private HashSet<Collider2D> _currentDetectionHitTriggers = new HashSet<Collider2D>();

		private HashSet<Collider2D> _currentDetectionHitColliders = new HashSet<Collider2D>();

		private Vector2 _raycastDirection;

		#region Properties
		public Vector2 velocity
		{
			get
			{
				return _velocity;
			}
			set
			{
				_velocity = value;
			}
		}

		public float velocityX
		{
			get
			{
				return _velocity.x;
			}
			set
			{
				_velocity.x = value;
			}
		}

		public float velocityY
		{
			get
			{
				return _velocity.y;
			}
			set
			{
				_velocity.y = value;
			}
		}

		public CollisionInfo collisionInfo
		{
			get
			{
				return _collisionInfo;
			}
		}
		#endregion

		protected override void Awake()
		{
			base.Awake();
			_physicsManager = JPhysicsManager.instance;
			_physicsManager.PushRigidbody( this );
			this.collisionMask = _physicsManager.setting.GetCollisionMask( this.gameObject.layer );

			// Add myself's collider to ignoredColliders list
			_ignoredColliders.Add( this.collider );
		}

		private void OnDestroy()
		{
			_physicsManager.RemoveRigidbody( this );
		}

		public override void Simulate( float deltaTime )
		{
			base.Simulate( deltaTime );

			// Add velocity generated by gravity
			var gravity = _physicsManager.setting.gravity;
			var gravityRatio = gravityScale * deltaTime;
			_velocity.x += gravity.x * gravityRatio;
			_velocity.y += gravity.y * gravityRatio;

			// Movement
			_movement.x = _velocity.x * deltaTime;
			_movement.y = _velocity.y * deltaTime;

			// Reset Collision Info Before Collision
			this.ResetStatesBeforeCollision();

			// Move
			this.Move( _movement );

			// Fix Insertion
			this.FixInsertion();

			//// Landing Platform
			//if( !_collisionInfo.isBelowCollision )
			//{
			//	_landingPlatform = null;
			//}

			// Fix Velocity
			this.FixVelocity();

			// Reset Collision Info After Collision
			this.ResetStatesAfterCollision();
		}

		private void ResetStatesBeforeCollision()
		{
			_colliderIsTrigger = this.collider.isTrigger;
			_collisionInfo.Reset();
			_triggerInfo.Reset();
			_raycastOrigins.Reset();
		}

		private void ResetStatesAfterCollision()
		{
		}

		public void Move( Vector2 movement )
		{
			if( this.collider == null || !this.collider.enabled )
			{
				return;
			}

			_movement.x = movement.x;
			_movement.y = movement.y;

			this.CollisionDetect();

			this.MovePosition( ref _movement );
		}

		private void FixInsertion()
		{
			_currentDetectionHitColliders.Clear();
		}

		private void FixVelocity()
		{
			// The Horizontal velocity should be zero if the rigidbody is facing some 'solid' collider.
			if( ( _velocity.x < 0.0f && _collisionInfo.isLeftCollision )
				|| ( _velocity.x > 0.0f && _collisionInfo.isRightCollision ) )
			{
				_velocity.x = 0.0f;
			}

			// The Vertical velocity should be zero if the rigidbody is facing some 'solid' collider.
			if( ( _velocity.y > 0.0f && _collisionInfo.isAboveCollision )
				|| ( _velocity.y < 0.0f && _collisionInfo.isBelowCollision ) )
			{
				_velocity.y = 0.0f;
			}
		}

		private void CollisionDetect()
		{
			// Clear Hit Triggers
			_currentDetectionHitTriggers.Clear();
			_currentDetectionHitColliders.Clear();

			// Prepare Collision Info
			this.PrepareCollisionInfo();

			if( float.IsNaN( _movement.x ) )
			{
				_movement.x = 0.0f;
			}
			if( float.IsNaN( _movement.y ) )
			{
				_movement.y = 0.0f;
			}

			// Horizontal
			this.HorizontalCollisionDetect();

			// Vertical
			this.VerticalCollisionDetect();
		}

		private void MovePosition( ref Vector3 movement )
		{
			if( float.IsNaN( movement.x ) )
			{
				movement.x = 0.0f;
			}

			if( float.IsNaN( movement.y ) )
			{
				movement.y = 0.0f;
			}

			_transform.position += movement;
		}

		private void PrepareCollisionInfo()
		{
			this.UpdateRaycastOrigins();
		}

		private void HorizontalCollisionDetect()
		{
			if( _movement.x == 0 )
			{
				return;
			}

			var directionX = _movement.x >= 0 ? 1 : -1;
			var rayOrigin = ( directionX == 1 ) ? _raycastOrigins.bottomRight : _raycastOrigins.bottomLeft;

			//因为起点往里缩了,所以此时射线长度应该加上这个内缩的距离
			var rayLength = Mathf.Abs( _movement.x ) + _shrinkWidth;

			for( int i = 0; i < this.horizontalRayCount; i++ )
			{
				_raycastDirection.x = 1.0f;
				_raycastDirection.y = 0.0f;
				_raycastDirection.x *= directionX;
				_raycastDirection.y *= directionX;

				var hitCount = Physics2D.RaycastNonAlloc( rayOrigin, _raycastDirection, _raycastHit2D, rayLength, this.collisionMask );
				for( int j = 0; j < hitCount; j++ )
				{
					var hit = _raycastHit2D[j];
					if( _ignoredColliders.Contains( hit.collider ) )
					{
						continue;
					}
					HandleHorizontalHitResult( hit, directionX );
				}
				rayOrigin.y += _horizontalRaySpace;
			}
		}

		private void HandleHorizontalHitResult( RaycastHit2D hit, int directionX )
		{
			var myLayer = this.gameObject.layer;
			var hitCollider = hit.collider;

			//Trigger
			if( HitTrigger( hit, directionX, null ) )
			{
				return;
			}

			// Collision Info
			_collisionInfo.collider = this.collider;
			_collisionInfo.hitCollider = hitCollider;
			_collisionInfo.position = hit.point;

			// Collision Direction
			var hitDistance = hit.distance;
			if( directionX == -1 )
			{
				_collisionInfo.isLeftCollision = true;
			}
			if( directionX == 1 )
			{
				_collisionInfo.isRightCollision = true;
			}

			//Push Collision 
			if( !_currentDetectionHitColliders.Contains( hitCollider ) )
			{
				_physicsManager.PushCollision( _collisionInfo );
				_currentDetectionHitColliders.Add( hitCollider );
			}

			//Fix movement
			if( _movement.x != 0.0f )
			{
				if( Mathf.Abs( hitDistance - _shrinkWidth ) < Mathf.Abs( _movement.x ) )
				{
					_movement.x = ( hitDistance - _shrinkWidth ) * directionX;
				}
			}
		}

		private void VerticalCollisionDetect()
		{
			var directionY = _movement.y > 0 ? 1 : -1;
			var rayOrigin = ( directionY == 1 ) ? _raycastOrigins.topLeft : _raycastOrigins.bottomLeft;
			rayOrigin.x += _movement.x;

			var rayLength = Mathf.Abs( _movement.y ) + _shrinkWidth;
			for( int i = 0; i < this.verticalRayCount; i++ )
			{
				_raycastDirection.x = 0.0f;
				_raycastDirection.y = 1.0f;
				_raycastDirection.x *= directionY;
				_raycastDirection.y *= directionY;
				var hitCount = Physics2D.RaycastNonAlloc( rayOrigin, _raycastDirection, _raycastHit2D, rayLength, this.collisionMask );
				for( int j = 0; j < hitCount; j++ )
				{
					var hit = _raycastHit2D[j];

					var hitCollider = hit.collider;
					// Ignored Collider?
					if( _ignoredColliders.Contains( hitCollider ) )
					{
						continue;
					}
					HandleVerticalHitResult( hit, directionY );
				}

				rayOrigin.x += _verticalRaySpace;
			}
		}

		private void HandleVerticalHitResult( RaycastHit2D hit, int directionY )
		{
			var myLayer = this.gameObject.layer;
			var hitCollider = hit.collider;
			// Trigger?
			if( HitTrigger( hit, null, directionY ) )
			{
				return;
			}

			// Collision Info
			_collisionInfo.collider = this.collider;
			_collisionInfo.hitCollider = hitCollider;
			_collisionInfo.position = hit.point;

			var hitDistance = hit.distance;
			// Collision Direction
			if( directionY == -1 )
			{
				_collisionInfo.isBelowCollision = true;
			}
			if( directionY == 1 )
			{
				_collisionInfo.isAboveCollision = true;
			}

			// Need Push Collision ?
			if( !_currentDetectionHitColliders.Contains( hitCollider ) )
			{
				_physicsManager.PushCollision( _collisionInfo );
				_currentDetectionHitColliders.Add( hitCollider );
			}

			//Fix movement
			if( _movement.y != 0.0f )
			{
				if( Mathf.Abs( hitDistance - _shrinkWidth ) < Mathf.Abs( _movement.y ) )
				{
					_movement.y = ( hitDistance - _shrinkWidth ) * directionY;
				}
			}
		}

		private bool HitTrigger( RaycastHit2D hit, int? directionX, int? directionY )
		{
			var hitCollider = hit.collider;
			// Trigger?
			if( hitCollider.isTrigger || _colliderIsTrigger )
			{
				_triggerInfo.collider = this.collider;
				_triggerInfo.hitCollider = hitCollider;
				_triggerInfo.position.x = hit.point.x;
				_triggerInfo.position.y = hit.point.y;
				if( directionY.HasValue )
				{
					if( directionY.Value == -1 )
					{
						_triggerInfo.isBelowCollision = true;
					}
					if( directionY.Value == 1 )
					{
						_triggerInfo.isAboveCollision = true;
					}
				}
				if( directionX.HasValue )
				{
					if( directionX.Value == -1 )
					{
						_triggerInfo.isLeftCollision = true;
					}
					if( directionX.Value == 1 )
					{
						_triggerInfo.isRightCollision = true;
					}
				}
				if( !_currentDetectionHitTriggers.Contains( hitCollider ) )
				{
					_physicsManager.PushCollision( _triggerInfo );
					_currentDetectionHitTriggers.Add( hitCollider );
				}
				return true;
			}
			return false;
		}
	}
}
