package io.github.ldev22.entity;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.Input;
import com.badlogic.gdx.graphics.glutils.ShapeRenderer;
import com.badlogic.gdx.math.Circle;
import io.github.ldev22.config.GameConfig;

public class Player {
    private float x = 0f;
    private float y = 0f;

    private Circle bounds;

    public Player(){
        bounds = new Circle(x, y , GameConfig.BOUNDS_RADIUS);
    }

    public void setPosition(float x, float y){
        this.x = x;
        this.y = y;
    }

    public void drawDebug(ShapeRenderer renderer){
        renderer.begin(ShapeRenderer.ShapeType.Filled);
        renderer.circle(bounds.x, bounds.y, bounds.radius, 30);
        renderer.end();
    }

    public void update(){
        float xSpeed = 0f;

        if(Gdx.input.isKeyPressed(Input.Keys.RIGHT)){
            xSpeed = GameConfig.MAX_X_SPEED;
        } else if (Gdx.input.isKeyPressed(Input.Keys.LEFT)) {
            xSpeed = -GameConfig.MAX_X_SPEED;
        }

        x += xSpeed;
        updateBounds();
    }
    private void updateBounds(){
        bounds.setPosition(x, y);
    }
}
