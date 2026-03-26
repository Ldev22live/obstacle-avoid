package io.github.ldev22.utils;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.graphics.OrthographicCamera;
import com.badlogic.gdx.math.Vector2;
import io.github.ldev22.config.GameConfig;

public class DebugCameraController {
    private Vector2 position;
    private Vector2 startPosition;

    public void setStartPosition(float x, float y){
        startPosition.set(x, y);
        position.set(x,y);
    }

    public void applyToCamera(OrthographicCamera camera){
        camera.position.set(position, 0f);
        camera.update();
    }

    public void handleDebugInput(){
        float delta = Gdx.graphics.getDeltaTime();
        float moveSpeed = GameConfig.DEFAULT_MOVE_SPEED * delta;

        if(Gdx.input.isKeyPressed(GameConfig.DEFAULT_LEFT_KEY)){
            moveLeft(moveSpeed);
        } else if (Gdx.input.isKeyPressed(GameConfig.DEFAULT_RIGHT_KEY)) {
            moveRight(moveSpeed);
        } else if (Gdx.input.isKeyPressed(GameConfig.DEFAULT_UP_KEY)) {
            moveUp(moveSpeed);
        } else if (Gdx.input.isKeyPressed(GameConfig.DEFAULT_DOWN_KEY)) {
            moveDown(moveSpeed);
        }
    }

    private void moveCamera(float xSpeed, float ySpeed){
        setPosition(position.x + xSpeed, position.y + ySpeed);
    }

    private void setPosition(float x, float y){
        position.set(x , y);
    }

    private void moveLeft(float moveSpeed){
        moveCamera(-moveSpeed, 0f);
    }

    private void moveRight(float moveSpeed){
        moveCamera(moveSpeed, 0f);
    }

    private void moveUp(float moveSpeed){
        moveCamera(0f, moveSpeed);
    }

    private void moveDown(float moveSpeed){
        moveCamera(0f, -moveSpeed);
    }
}
