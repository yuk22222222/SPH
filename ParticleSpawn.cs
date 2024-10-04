using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

public class ParticleSpawn : MonoBehaviour
{   public GameObject particlePrefab; // assign the prefab in inspector
    public int rows = 25;    
    public int columns = 25; 
    public float spacing = 0.5f; // space between particles
    private Vector2[] particlePositions; 
    private GameObject[] particleReference; // private array to keep track of particles
    void Start()
    {
        particlePositions = new Vector2[rows * columns]; 
        particleReference = new GameObject[rows * columns];
        SpawnParticles();
        
    }

   void SpawnParticles()
    {
        Vector3 startPosition = transform.position; // start position
        int index = 0; //array index

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                // find the position for each particle
                float posX = startPosition.x + column * spacing;
                float posY = startPosition.y + row * spacing;

                Vector3 spawnPosition = new Vector3(posX, posY, startPosition.z);
                
                //store the positions
                particlePositions[index] = new Vector2(posX, posY);
                
                // instantiate the particle 
                GameObject particle = Instantiate(particlePrefab, spawnPosition, Quaternion.identity);
                particleReference[index] = particle;

                index++;
            }
        }
    }
    public Vector2[] getPositionArray(){
        return particlePositions;
    }
    public Vector2 getPosition(int index){
        return particlePositions[index];
    }

    public void setPosition(Vector2 position, int index){
        particlePositions[index]=position;
    }

    public int getNumParticle( ){
        return rows*columns;
    }

    public GameObject getParticleReference(int index){
        return particleReference[index];
    }
}
