using System.Collections.Generic;
using Godot;

// Applies instantaneous spherical damage through the existing IDamageable interface.
// Collects recipients before applying damage and deduplicates shield/hull overlaps.
public static class AttackAreaDamage
{
	#region Burst Resolution

		// =========================================================
	// Applies one typed spherical burst after resolving cover and distance falloff.
	public static int ApplyBurst(
		Node3D context,
		AttackAreaDefinition definition,
		Vector3 position,
		float damage,
		float areaScale,
		CollisionObject3D source,
		Faction sourceFaction,
		DamageType damageType = DamageType.Neutral
	)
	{
		if (!GodotObject.IsInstanceValid(context)
			|| definition == null
			|| damage <= 0.0f
			|| definition.Radius <= 0.0f)
		{
			return 0;
		}

		float radius = definition.Radius
			* Mathf.Max(0.01f, areaScale);

		PhysicsDirectSpaceState3D space =
			context.GetWorld3D().DirectSpaceState;

		using SphereShape3D sphere = new()
		{
			Radius = radius
		};

		using PhysicsShapeQueryParameters3D query = new()
		{
			Shape = sphere,
			Transform = new Transform3D(Basis.Identity, position),
			CollisionMask = definition.TargetMask,
			CollideWithBodies = true,
			CollideWithAreas = false
		};

		List<CollisionObject3D> recipients =
			CollectRecipients(space, query);

		List<PendingHit> pending = new();

		foreach (CollisionObject3D recipient in recipients)
		{
			if (!CanDamage(
				recipient,
				definition,
				source,
				sourceFaction
			))
			{
				continue;
			}

			Vector3 offset = recipient.GlobalPosition - position;
			float distance = offset.Length();

			float ratio = Mathf.Clamp(
				distance / radius,
				0.0f,
				1.0f
			);

			float multiplier = Mathf.Lerp(
				1.0f,
				Mathf.Clamp(definition.FalloffMinimum, 0.0f, 1.0f),
				Mathf.Pow(
					ratio,
					Mathf.Max(0.01f, definition.FalloffExponent)
				)
			);

			if (definition.CheckCover
				&& IsCovered(
					space,
					definition,
					position,
					recipient,
					source
				))
			{
				multiplier *= Mathf.Clamp(
					definition.BlockedDamageMultiplier,
					0.0f,
					1.0f
				);
			}

			float amount = damage * multiplier;

			if (amount <= 0.0f)
			{
				continue;
			}

			Vector3 normal = distance > 0.001f
				? -offset / distance
				: Vector3.Up;

			pending.Add(new PendingHit(
				recipient,
				amount,
				normal
			));
		}

		int damaged = 0;

		foreach (PendingHit hit in pending)
		{
			if (!GodotObject.IsInstanceValid(hit.Recipient)
				|| hit.Recipient.IsQueuedForDeletion())
			{
				continue;
			}

			if (hit.Recipient is IDamageable receiver)
			{
				receiver.ApplyDamage(new DamageInfo(
					hit.Amount,
					GodotObject.IsInstanceValid(source) ? source : null,
					sourceFaction,
					position,
					hit.Normal,
					damageType
				));

				damaged++;
			}
		}

		return damaged;
	}

	#endregion

	#region Candidate Collection

	// =========================================================
	// Collects overlapping bodies and maps shields to their owning ship.
	private static List<CollisionObject3D> CollectRecipients(
		PhysicsDirectSpaceState3D space,
		PhysicsShapeQueryParameters3D query
	)
	{
		List<CollisionObject3D> recipients = new();
		HashSet<ulong> seen = new();

		// Page through results by excluding bodies already collected.
		// This avoids silently truncating explosions in crowded areas.
		Godot.Collections.Array<Rid> excluded = new();

		const int batchSize = 64;

		while (true)
		{
			query.Exclude = excluded;

			var results = space.IntersectShape(query, batchSize);

			if (results.Count == 0)
			{
				break;
			}

			int previousExcludedCount = excluded.Count;

			foreach (var result in results)
			{
				Rid rid = result["rid"].AsRid();

				if (!excluded.Contains(rid))
				{
					excluded.Add(rid);
				}

				CollisionObject3D collider =
					result["collider"].AsGodotObject()
					as CollisionObject3D;

				if (!GodotObject.IsInstanceValid(collider))
				{
					continue;
				}

				CollisionObject3D recipient =
					collider is ShipShield shield
						? shield.Ship
						: collider;

				if (!GodotObject.IsInstanceValid(recipient)
					|| recipient is not IDamageable)
				{
					continue;
				}

				if (seen.Add(recipient.GetInstanceId()))
				{
					recipients.Add(recipient);
				}
			}

			if (results.Count < batchSize
				|| excluded.Count == previousExcludedCount)
			{
				break;
			}
		}

		return recipients;
	}

	#endregion

	#region Target Filtering And Cover

	// =========================================================
	// Applies ownership, faction, and environment rules to one recipient.
	private static bool CanDamage(
		CollisionObject3D recipient,
		AttackAreaDefinition definition,
		CollisionObject3D source,
		Faction sourceFaction
	)
	{
		if (!GodotObject.IsInstanceValid(recipient)
			|| recipient.IsQueuedForDeletion())
		{
			return false;
		}

		if (recipient == source)
		{
			return definition.DamageSource;
		}

		if (recipient is ICombatTarget target)
		{
			if (!target.IsCombatTargetable)
			{
				return false;
			}

			return definition.FriendlyFire
				|| target.CombatFaction != sourceFaction;
		}

		return definition.DamageEnvironment;
	}

	// =========================================================
	// Tests solid cover between the blast origin and the recipient's centre.
	private static bool IsCovered(
		PhysicsDirectSpaceState3D space,
		AttackAreaDefinition definition,
		Vector3 position,
		CollisionObject3D recipient,
		CollisionObject3D source
	)
	{
		Vector3 destination = recipient.GlobalPosition;
		Vector3 offset = destination - position;

		if (offset.LengthSquared() < 0.0001f)
		{
			return false;
		}

		Godot.Collections.Array<Rid> excluded = new()
		{
			recipient.GetRid()
		};

		if (GodotObject.IsInstanceValid(source)
			&& source != recipient)
		{
			excluded.Add(source.GetRid());
		}

		using PhysicsRayQueryParameters3D ray = new()
		{
			// Move slightly toward the recipient to avoid surface-contact noise.
			From = position + offset.Normalized() * 0.02f,
			To = destination,
			CollisionMask = definition.CoverMask,
			CollideWithBodies = true,
			CollideWithAreas = false,
			HitFromInside = false,
			Exclude = excluded
		};

		return space.IntersectRay(ray).Count > 0;
	}

	#endregion

	#region Pending Damage

	// Stores resolved damage until all cover checks have finished.
	private readonly struct PendingHit
	{
		public readonly CollisionObject3D Recipient;
		public readonly float Amount;
		public readonly Vector3 Normal;

		// =========================================================
		// Records one recipient and its resolved burst damage.
		public PendingHit(
			CollisionObject3D recipient,
			float amount,
			Vector3 normal
		)
		{
			Recipient = recipient;
			Amount = amount;
			Normal = normal;
		}
	}

	#endregion
}