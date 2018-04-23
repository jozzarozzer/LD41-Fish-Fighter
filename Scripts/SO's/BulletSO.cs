﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBullet", menuName = "Action/Bullet")]
public class BulletSO : ScriptableObject
{
    public string bulletType;
    public float damage;

    public GameObject bulletObj;
    public Mesh bulletMesh;

    //public sprite/mesh
}
