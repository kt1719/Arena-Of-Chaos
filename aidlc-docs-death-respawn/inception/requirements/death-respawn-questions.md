# Player Death & Respawn — Verification Questions

Please answer the following questions to help clarify the requirements.

## Question 1
When the player dies, should the weapon (spawned under WeaponParent) also be hidden, or should it remain visible?

A) Hide the weapon along with the player visuals
B) Keep the weapon visible (only hide the player body/shadow/trail)
C) Other (please describe after [Answer]: tag below)

[Answer]: A

## Question 2
The HurtBox child object has a BoxCollider2D (trigger) used for damage detection. Should this be disabled on death in addition to the main CapsuleCollider2D?

A) Yes, disable both the HurtBox BoxCollider2D and the main CapsuleCollider2D on death
B) Only disable the main CapsuleCollider2D (current behavior)
C) Other (please describe after [Answer]: tag below)

[Answer]: A

## Question 3
On respawn, should the player have a brief invincibility period (i-frames) to prevent being killed immediately after respawning?

A) No invincibility — respawn and immediately be vulnerable
B) Yes — add a short invincibility window (1-2 seconds) after respawn
C) Other (please describe after [Answer]: tag below)

[Answer]: B
