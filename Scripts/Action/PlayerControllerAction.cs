﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerAction : MonoBehaviour {

    public GameObject mesh;
    public GameObjectVariable playerGameObjectVariable;

    public float screenShakeMultiplier = 1;

    [Header("Targeting")]
    public LayerMask groundMask;
    public LayerMask obstacleMask;
    public GameObject targetingReticle;
    GameObject currentTarget;
    Vector3 targetPosition;
    Vector3 mousePosition; //position that the mouse ray collides with the ground.

    [Header("Stats")]
    public int energy;
    public IntVariable energyVariable;
    public int maxEnergy;
    public IntVariable maxEnergyVariable;

    public int health;
    public IntVariable healthVariable;
    public int maxHealth;
    public IntVariable maxHealthVariable;

    float rechargeTimePassed;
    public float rechargeTick;
    public int rechargeAmount;


    [Header("Attacks")]
    public GunSO gun;
    public GunArrayVariable playerGuns;

    //public Attack playerAttack1;
    //public GameObject attack1Particle;
    //public ParticleSystem attack1ParticleSys;

    public GameObject gunHolder;
    public GameObject bulletObject;
    public GameObject bulletSpawnPoint;

    bool currentlyShooting;
    public float gunRecoil;
    public int gunEnergyCost;

    bool invincible;
    public float invincibilityTime;

    [Header("Walking")]
    public float speedMult;
    bool currentlyWalking;
    Vector3 rawMovementInput;

    [Header("Dashing")]
    public float dashMaxDistance;
    public float dashMaxTime;
    float dashSpeed;
    public int dashEnergyCost;
    bool currentlyDashing;
    public TrailRenderer trail;
    public Color trailColour;

    [Header("Camera")]
    public Camera cam;

    Rigidbody rigidBody;

    int checkNum; //for checking the amount of bools true in a canX check.

    GameObject[] projectileArray;

    public GameObject GameOverUI;

    [Header("debug")]
    Vector3 hitPoint;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();

        currentlyDashing = false;
        currentlyShooting = false;
        currentlyWalking = false;

        health = maxHealth;
        energy = maxEnergy;

        rechargeTimePassed = 0;

        trail.startColor = Color.clear;
        trail.endColor = Color.clear;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Die();
        }

        if (health <= 0)
        {
            Die();
        }

        transform.position = new Vector3(transform.position.x, transform.position.y * 0, transform.position.z);
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

        DetermineGun();

        EnergyRecharge();

        healthVariable.value = health;
        energyVariable.value = energy;
        maxHealthVariable.value = maxHealth;
        maxEnergyVariable.value = maxEnergy;


        playerGameObjectVariable.value = gameObject;

        DetermineMousePosition();

        Walk();

        Targeting();

        GunSetup();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (CanDash())
            {
                StartCoroutine(Dash(mousePosition, 1, 1));
            }

        }

        if (Input.GetMouseButtonDown(0))
        {
            if (CanAttack1())
            {
                Attack1();
            }
        }
    }

    void Die()
    {
        GameOverUI.SetActive(true);
        Destroy(gameObject);
    }

    void DetermineGun()
    {
        if (playerGuns.value[0] != null)
        {
            for (int i = 0; i < playerGuns.value.Length; i++)
            {
                if (gun != null)
                {
                    if (playerGuns.value[i] != null && playerGuns.value[i].level > gun.level)
                    {
                        gun = playerGuns.value[i];
                    }
                }
                else
                {
                    gun = playerGuns.value[0];
                }

            }
        }
    }

    void EnergyRecharge()
    {
        if (energy < maxEnergy && rechargeTimePassed >= rechargeTick)
        {
            energy += rechargeAmount;
            rechargeTimePassed = 0;
        }

        rechargeTimePassed += Time.deltaTime;
    }

    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.layer != 8)
        {
            print("E collision");
        }
        if (collision.gameObject.GetComponent<EnemyController>())
        {
            print("E collision enemy");
            var enemyScript = collision.gameObject.GetComponent<EnemyController>();
            if (!invincible)
            {
                print("E damaged");
                invincible = true;
                takeDamage(enemyScript.enemyData.damage);
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }
    }



    IEnumerator Invincibility()
    {
        invincible = true;
        int loopCount = 5;
        for (int i = 0; i < loopCount; i++)
        {
            mesh.GetComponent<MeshRenderer>().enabled = false;
            yield return new WaitForSeconds(invincibilityTime / (2 * loopCount));
            mesh.GetComponent<MeshRenderer>().enabled = true;
            yield return new WaitForSeconds(invincibilityTime / (2 * loopCount));
        }
        invincible = false;
    }

    void takeDamage(int damage)
    {
        StartCoroutine(Invincibility());
        health -= damage;
    }

    void GunSetup()
    {
        bulletObject = gun.bulletType.bulletObj;
        gunRecoil = gun.recoil;
    }

    void DetermineMousePosition() //determines the mouse position as a ray from screen to ground. 
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition); //cast a ray from the camera based on the mouse position
        RaycastHit hit;
        Physics.Raycast(ray, out hit, 1000, groundMask);

        mousePosition = hit.point;
        hitPoint = hit.point;


        Vector3 vector1 = hit.point + transform.position;

        /*
        RaycastHit hit2;
        if (Physics.Raycast(transform.position, vector1.normalized * dashMaxDistance, out hit2, obstacleMask))
        {
            mousePosition = hit2.point;
        }
        */
    }

    void Walk() //Checks if the player can move, and then lerps slightly to the desired velocity. 
    {
        rawMovementInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (CanWalk())
        {
            if (rawMovementInput != Vector3.zero)
            {
                currentlyWalking = true;
                Vector3 movementVector = rawMovementInput.normalized * speedMult;
                rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, movementVector, 0.2f);
            }
            else
            {
                currentlyWalking = false;
                rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, Vector3.zero, 0.2f);
            }
        }
        else
        {
            currentlyWalking = false;
        }
    }

    void Targeting() //controls the targeting reticle, and points Attack1 in the right direction. 
    {
        targetingReticle.transform.position = mousePosition;
    
        gunHolder.transform.LookAt(mousePosition);

        Vector3 velocityRaised = new Vector3(mousePosition.x, transform.position.y, mousePosition.z); //hit point raised to y of player
        Vector3 velocityPosition = rigidBody.velocity + transform.position;
        transform.LookAt(velocityPosition);
        //transform.eulerAngles += initialRotation;
    }

    bool CanDash() //Checks if the player is able to dash. 
    {
        checkNum = 0;
        if (energy >= dashEnergyCost) checkNum++;
        if (!currentlyDashing) checkNum++;

        if (checkNum == 2) return true;
        else return false;

    }

    bool CanWalk() //Checks if the player is able to move. 
    {
        checkNum = 0;
        if (!currentlyDashing) checkNum++;
        if (!currentlyShooting) checkNum++;

        if (checkNum == 2) return true;
        else return false;
    }

    bool CanAttack1() //Checks if the player is able to use Attack1. 
    {
        checkNum = 0;
        if (energy >= gunEnergyCost) checkNum++;
        if (!currentlyDashing) checkNum++;
        if (!currentlyShooting) checkNum++;

        if (checkNum == 3) return true;
        else return false;
    }

    void UseEnergy(int cost) //Uses inputed energy amount. 
    {
        energy -= cost;
    }

    void Recoil(float recoilAmount)
    {
        if (currentTarget != null)
        {
            Vector3 targetPos = currentTarget.transform.position;
            Vector3 targetPosGrounded = new Vector3(targetPos.x, transform.position.y, targetPos.z); //very hacky
            Vector3 inverseTargetPos = new Vector3(-targetPosGrounded.x, targetPosGrounded.y, -targetPosGrounded.z);

            Vector3 aimAt = inverseTargetPos + transform.position;

            rigidBody.velocity = aimAt.normalized * recoilAmount;
        }
        else
        {
            Vector3 inverseMousePos = new Vector3(-mousePosition.x, mousePosition.y, -mousePosition.z);

            Vector3 aimAt = inverseMousePos + transform.position;
            rigidBody.velocity = aimAt.normalized * recoilAmount;
        }
    }

    void Attack1()
    {
        Vector3 projectileVelocity = mousePosition - transform.position;

        Recoil(gunRecoil);
        UseEnergy(gunEnergyCost);


        GameObject projectile = Instantiate(bulletObject, bulletSpawnPoint.transform.position, bulletSpawnPoint.transform.rotation);
        projectile.GetComponent<ProjectileScript>().SetVelocity(projectileVelocity);
        projectile.GetComponent<ProjectileScript>().bulletData = gun.bulletType;
        projectile.GetComponent<AudioSource>().pitch += (Random.value - 0.5f) / 10;

        StartCoroutine(Screenshake());



        //projectile.GetComponent<ProjectileScript>().attackCollidersOn = true;
    }

    IEnumerator Screenshake()
    {
        Vector3 camInitialPos = cam.transform.position;
        for (int i = 0; i < 3; i++)
        {
            cam.transform.position = camInitialPos + Random.insideUnitSphere * screenShakeMultiplier;
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator Dash(Vector3 mousePos, int energyMult, float dashSpeedMult) //Makes the player dash. 
    {
        currentlyDashing = true;
        trail.startColor = trailColour;
        trail.endColor = trailColour;

        UseEnergy(dashEnergyCost * energyMult);

        Vector3 adjustedHitPoint = mousePos - transform.position; //moves the hit.point vector based on player position
        Vector3 aimVector = adjustedHitPoint.normalized * dashMaxDistance; //normalizes the adjusted hit.point and multiplies by the dashMaxDistance modifier
        Vector3 targetPosition = transform.position + aimVector; //adds the normalized vector to player position to get final position

        if (aimVector.magnitude > adjustedHitPoint.magnitude) //if the hit.point is less than the max dash distance, then just uses the hit.point as the target instead
        {
            targetPosition = mousePos;
        }

        dashSpeed = dashMaxDistance / dashMaxTime; //determining the speed of the dash.

        Vector3 targetSelfDifference = targetPosition - transform.position; //Vector between player position and target

        rigidBody.velocity = targetSelfDifference.normalized * dashSpeed * dashSpeedMult; //set velocity to previous vector normalized multiplied by the speed.

        float dashActualTime = 1 / (dashSpeed / targetSelfDifference.magnitude); //determine the time that the dash will take

        yield return new WaitForSeconds(dashActualTime * (1 / dashSpeedMult));

        currentlyDashing = false;

        StartCoroutine(TrailTimer(0.2f));
    }


    IEnumerator TrailTimer(float seconds) //waits for a time and then turns off the dash trail, unless you dash again. 
    {
        for (float i = 0; i < 30; i += 0.025f)
        {
            if (currentlyDashing)
            {
                trail.startColor = trailColour;
                trail.endColor = trailColour;
                break;
            }
            if (i >= seconds)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (currentlyDashing)
                    {
                        break;
                    }
                    if (j == 5)
                    {
                        trail.Clear();
                        trail.startColor = Color.clear;
                        trail.endColor = Color.clear;
                    }
                    else
                    {
                        trail.startColor = Vector4.Lerp(trail.startColor, Color.clear, 0.2f);
                        trail.endColor = Vector4.Lerp(trail.endColor, Color.clear, 0.2f);
                        yield return new WaitForSeconds(0.05f);
                    }
                }

                break;
            }
            yield return new WaitForSeconds(0.025f);
        }
    }

    void OnDrawGizmos()
    {
        Vector3 vector1 = mousePosition - transform.position;
        Vector3 vector2 = vector1.normalized * dashMaxDistance;
        Vector3 aimVector = transform.position + vector1.normalized * dashMaxDistance;

        if (vector2.magnitude > vector1.magnitude)
        {
            aimVector = mousePosition;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, mousePosition);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(mousePosition, 0.1f);

        Gizmos.DrawLine(cam.transform.position, hitPoint);
    }
}

