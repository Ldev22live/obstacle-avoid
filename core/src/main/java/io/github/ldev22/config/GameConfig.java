package io.github.ldev22.config;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.Input;

public class GameConfig {
    public static final int WIDTH = 480; // desktop only
    public static final int HEIGHT = 800; // desktop only
    public static final float WORLD_WIDTH = 6.0f;
    public static final float WORLD_HEIGHT = 10.0f;

    //game constants
    public static final float BOUNDS_RADIUS = 0.4f;
    public static final float SIZE = BOUNDS_RADIUS * 2;

    public static final float MAX_X_SPEED = 0.25f;
    public static final int DEFAULT_LEFT_KEY = Input.Keys.A;
    public static final int DEFAULT_RIGHT_KEY = Input.Keys.D;
    public static final int DEFAULT_UP_KEY = Input.Keys.W;
    public static final int DEFAULT_DOWN_KEY = Input.Keys.S;

    public static final float DEFAULT_MOVE_SPEED = 20f;

    public static final float WORLD_CENTER_X = WORLD_WIDTH / 2f
    public static final float WORLD_CENTER_Y = WORLD_HEIGHT / 2f
}
